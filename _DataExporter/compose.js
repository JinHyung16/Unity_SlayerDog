/**
 * 모듈 조립기.
 *
 * ── 런타임 실행 모델 (이것 때문에 트리 모양이 결정된다) ──
 * BT 노드들은 대부분 "SkillContext 에 값을 써두는 리프"다.
 *   Targeting -> ctx.Targets      Hit -> ctx.HitNode
 *   Debuff    -> ctx.DebuffNodes  Despawn -> ctx.DespawnNode
 * 마지막에 Launch 리프가 그 ctx 를 읽어서 실제 이펙트를 스폰한다.
 *
 * 중요: Launch 와 Trigger 는 **자식을 실행하지 않는다.** 그래서 Hit/Despawn 을
 * Launch 의 자식으로 매달면 영영 실행되지 않는다. 전부 형제로 나열하고 Launch 를 맨 뒤에 둬야 한다.
 * (Cooldown/Chance/Selector/Inverter/Parallel 은 자식을 실행하므로 중첩 가능)
 *
 * 그래서 모듈은 트리가 아니라 '형제 노드 목록(nodes)'을 내놓고,
 * 조립기가 Sequence 하나로 감싼다. 순서 = 실행 순서.
 */

// '@stat.Damage' — 참조한 모듈의 값
const TOKEN_PATTERN = /^@([a-zA-Z]+)\.([A-Za-z0-9_]+)$/;
// '#debuff/burn' — id 테이블 모듈을 경로로 참조하면 숫자 id 로 치환된다.
const ID_REF_PATTERN = /^#([a-zA-Z]+)\/(.+)$/;

// 레벨 곡선을 함수로 계산하면 0.55 가 0.5499999999999999 로 나온다.
function roundNumber(value)
{
    if (typeof value !== 'number' || Number.isFinite(value) === false)
    {
        return value;
    }
    return Math.round(value * 1e6) / 1e6;
}

function isKind(type, prefix)
{
    return typeof type === 'string' && type.startsWith(prefix);
}

class Composer
{
    constructor(enums, modules, config, log)
    {
        this._enums = enums;
        this._modules = modules; // { visual: Map, method: Map, buff: Map, debuff: Map, part/option/표 }
        this._unimplemented = new Set(config.UnimplementedNodes || []);
        this._log = log || (() => {});
        this._errors = [];
    }

    get errors()
    {
        return this._errors;
    }

    _fail(where, message)
    {
        this._errors.push(`${where}: ${message}`);
    }

    _hasEnumMember(enumName, member)
    {
        const members = this._enums[enumName];
        if (!members)
        {
            return false;
        }
        return members.includes(member);
    }

    /**
     * 'straight'            -> Skill/Method/straight
     * 'projectile/homing'   -> Skill/Method/projectile/homing
     * 'Skill/Method/x'      -> 그대로 (루트 상대 경로 표기)
     */
    _resolveRef(kind, ref, moduleDir, where)
    {
        if (typeof ref !== 'string' || ref.trim() === '')
        {
            return null;
        }

        const normalized = ref.trim().replace(/\\/g, '/').replace(/^\.\//, '');
        const fullId = normalized.startsWith(moduleDir + '/') ? normalized : `${moduleDir}/${normalized}`;
        const found = this._modules[kind].get(fullId);

        if (!found)
        {
            const candidates = [...this._modules[kind].keys()].join(', ');
            this._fail(where, `${kind} 모듈을 찾을 수 없습니다: '${ref}' (해석 결과 '${fullId}') / 존재하는 것: [${candidates}]`);
            return null;
        }

        return { id: fullId, data: found.data };
    }

    _resolveToken(value, bindings, where)
    {
        if (typeof value !== 'string')
        {
            return value;
        }

        const idRef = ID_REF_PATTERN.exec(value);
        if (idRef !== null)
        {
            const kind = idRef[1];
            if (!this._modules[kind])
            {
                this._fail(where, `알 수 없는 모듈 종류 '#${kind}/...' (사용 가능: ${Object.keys(this._modules).join(', ')})`);
                return value;
            }

            const resolved = this._resolveRef(kind, idRef[2], this._moduleDirs[kind], where);
            if (resolved === null)
            {
                return value;
            }

            const id = Number(resolved.data.id);
            if (!Number.isFinite(id) || id <= 0)
            {
                this._fail(where, `'${resolved.id}' 에 유효한 id 가 없습니다`);
                return value;
            }
            return id;
        }

        const match = TOKEN_PATTERN.exec(value);
        if (match === null)
        {
            if (value.startsWith('@') || value.startsWith('#'))
            {
                this._fail(where, `토큰 표기가 잘못됐습니다: '${value}' (형식: @stat.Key / @visual.Key / #debuff/path)`);
            }
            return value;
        }

        const scope = match[1];
        const key = match[2];
        const table = bindings[scope];

        if (!table)
        {
            this._fail(where, `알 수 없는 토큰 스코프 '${scope}' (사용 가능: ${Object.keys(bindings).join(', ')})`);
            return value;
        }
        if (Object.prototype.hasOwnProperty.call(table, key) === false)
        {
            this._fail(where, `'@${scope}.${key}' 를 찾을 수 없습니다 (${scope} 값: ${Object.keys(table).join(', ') || '없음'})`);
            return value;
        }

        return table[key];
    }

    /**
     * 슬롯 치환 + 토큰 해석 + enum 검증. 노드 하나가 0..N 개로 펼쳐질 수 있어 배열을 돌려준다.
     */
    _buildNodes(node, slots, bindings, where, usedSlots)
    {
        if (node === null || typeof node !== 'object')
        {
            this._fail(where, `노드가 객체가 아닙니다: ${JSON.stringify(node)}`);
            return [];
        }

        if (node.slot !== undefined)
        {
            const name = node.slot;
            usedSlots.add(name);

            const filled = slots[name];
            if (filled === undefined)
            {
                if (node.optional === true)
                {
                    return [];
                }
                this._fail(where, `슬롯 '${name}' 이 비었습니다 (스킬의 slots 에 채우거나 optional: true 를 주세요)`);
                return [];
            }

            const list = Array.isArray(filled) ? filled : [filled];
            const out = [];
            for (let i = 0; i < list.length; i++)
            {
                out.push(...this._buildNodes(list[i], slots, bindings, `${where} > slot:${name}[${i}]`, usedSlots));
            }
            return out;
        }

        const type = node.type;
        if (typeof type !== 'string' || type === '')
        {
            this._fail(where, 'node.type 이 없습니다');
            return [];
        }
        if (this._hasEnumMember('ESkillNodeType', type) === false)
        {
            this._fail(where, `ESkillNodeType 에 없는 노드 타입: '${type}'`);
        }
        else if (this._unimplemented.has(type))
        {
            this._fail(where, `'${type}' 은 런타임 미구현입니다 — 데이터로 쓰면 조용히 아무 일도 안 일어납니다`);
        }

        const built = { id: 0, type: type, params: {}, children: [] };

        const params = node.params || {};
        for (const key of Object.keys(params))
        {
            if (this._hasEnumMember('ESkillParamKey', key) === false)
            {
                this._fail(`${where} > ${type}`, `ESkillParamKey 에 없는 파라미터: '${key}'`);
            }

            const resolved = this._resolveToken(params[key], bindings, `${where} > ${type}.${key}`);
            if (resolved === null || resolved === undefined)
            {
                continue;
            }
            built.params[key] = roundNumber(resolved);
        }

        const children = node.children || [];
        if (Array.isArray(children) === false)
        {
            this._fail(`${where} > ${type}`, 'children 은 배열이어야 합니다');
        }
        else
        {
            for (let i = 0; i < children.length; i++)
            {
                built.children.push(...this._buildNodes(children[i], slots, bindings, `${where} > ${type}[${i}]`, usedSlots));
            }
        }

        return [built];
    }

    _assignIds(node, counter)
    {
        node.id = counter.next++;
        for (const child of node.children)
        {
            this._assignIds(child, counter);
        }
        return node;
    }

    /**
     * 실행 순서대로(전위 순회) 펼친다. 순수 Sequence 트리에서는 이게 곧 tick 순서다.
     */
    _flatten(node, out)
    {
        out.push(node);
        for (const child of node.children)
        {
            this._flatten(child, out);
        }
        return out;
    }

    /**
     * 컴파일은 되는데 런타임에 조용히 아무 일도 안 하는 배치를 잡는다.
     * 여기서 안 막으면 "데이터는 맞는데 스킬이 안 나가요"로 몇 시간 날아간다.
     */
    _validateStructure(root, where)
    {
        const flat = this._flatten(root, []);

        const launches = [];
        let hits = 0;
        let despawns = 0;

        for (let i = 0; i < flat.length; i++)
        {
            const type = flat[i].type;
            if (isKind(type, 'Launch'))
            {
                launches.push(i);
            }
            if (isKind(type, 'Hit'))
            {
                hits++;
            }
            if (isKind(type, 'Despawn'))
            {
                despawns++;
            }
        }

        if (launches.length > 1)
        {
            this._fail(where, `Launch 노드가 ${launches.length}개입니다 — 스킬 하나에 하나만 둘 수 있습니다`);
        }
        else if (launches.length === 1 && launches[0] !== flat.length - 1)
        {
            const after = flat.slice(launches[0] + 1).map(n => n.type).join(', ');
            this._fail(where, `Launch 노드 뒤에 [${after}] 가 있습니다 — Launch 는 ctx 를 읽기만 하므로 반드시 마지막이어야 합니다`);
        }

        // ctx.HitNode / ctx.DespawnNode 는 단일 슬롯이라 뒤에 오는 게 앞을 덮어쓴다
        if (hits > 1)
        {
            this._fail(where, `Hit 노드가 ${hits}개입니다 — ctx.HitNode 는 하나뿐이라 마지막 것만 적용됩니다`);
        }
        if (despawns > 1)
        {
            this._fail(where, `Despawn 노드가 ${despawns}개입니다 — ctx.DespawnNode 는 하나뿐이라 마지막 것만 적용됩니다`);
        }
    }

    _buildLevels(statData, maxLevel, where)
    {
        const raw = statData ? statData.levels : null;
        if (!raw)
        {
            return [];
        }

        const out = [];

        if (typeof raw === 'function')
        {
            for (let level = 1; level <= maxLevel; level++)
            {
                out.push({ level: level, modifiers: this._sanitizeModifiers(raw(level), where) });
            }
            return out;
        }

        if (Array.isArray(raw))
        {
            for (const row of raw)
            {
                const level = Number(row.level);
                if (!Number.isFinite(level) || level < 1)
                {
                    this._fail(where, `levels 항목에 유효한 level 이 없습니다: ${JSON.stringify(row)}`);
                    continue;
                }
                const modifiers = Object.assign({}, row);
                delete modifiers.level;
                out.push({ level: level, modifiers: this._sanitizeModifiers(modifiers, where) });
            }
            out.sort((a, b) => a.level - b.level);
            return out;
        }

        this._fail(where, 'levels 는 함수 또는 배열이어야 합니다');
        return [];
    }

    _sanitizeModifiers(modifiers, where)
    {
        const out = {};
        for (const key of Object.keys(modifiers || {}))
        {
            if (this._hasEnumMember('ESkillParamKey', key) === false)
            {
                this._fail(where, `levels 에 ESkillParamKey 가 아닌 키: '${key}'`);
                continue;
            }
            out[key] = roundNumber(modifiers[key]);
        }
        return out;
    }

    compose(entry, moduleDirs)
    {
        const skill = entry.data;
        const where = entry.rel;

        this._moduleDirs = moduleDirs;

        const id = Number(skill.id);
        if (!Number.isFinite(id) || id <= 0)
        {
            this._fail(where, `id 가 없거나 유효하지 않습니다: ${JSON.stringify(skill.id)}`);
            return null;
        }

        // Visual 은 **스킬과 같은 이름**으로 찾는다. 이름이 곧 연결이라 Master 에 적지 않는다 —
        // 적게 두면 이름과 참조가 어긋날 수 있고, 어긋나면 조용히 남의 그림이 나간다.
        //
        // 옛 stat 은 Visual 로 합쳤다. 그래서 `@stat.Damage` 와 `@visual.Impact` 가
        // **같은 파일**을 본다. 토큰 이름은 남겨 둔다 — 수치와 그림은 읽는 사람에게 다른 것이다.
        const skillName = entry.id.slice(entry.id.lastIndexOf('/') + 1);
        const visual = this._resolveRef('visual', skillName, moduleDirs.visual, where);
        const method = this._resolveRef('method', skill.method, moduleDirs.method, where);

        const values = Object.assign({},
            visual ? (visual.data.values || {}) : {},
            (skill.overrides && skill.overrides.stat) || {},
            (skill.overrides && skill.overrides.visual) || {});

        // 두 스코프가 같은 표를 본다. 합쳤으므로 나눌 이유가 없다.
        const bindings = { stat: values, visual: values };
        const slots = skill.slots || {};
        const usedSlots = new Set();

        // 본체 노드 목록: 스킬이 nodes 를 직접 주면 그걸 쓰고, 아니면 Method 모듈 것
        let bodyNodes = null;
        if (Array.isArray(skill.nodes))
        {
            bodyNodes = skill.nodes;
        }
        else if (method)
        {
            bodyNodes = method.data.nodes;
            if (Array.isArray(bodyNodes) === false)
            {
                this._fail(where, `method 모듈 '${method.id}' 에 nodes 배열이 없습니다`);
                return null;
            }
        }
        else
        {
            this._fail(where, 'method 참조도 nodes 도 없습니다');
            return null;
        }

        const trigger = skill.trigger;
        if (typeof trigger !== 'string' || trigger === '')
        {
            this._fail(where, 'trigger 가 없습니다');
            return null;
        }

        // Sequence 하나 아래에 형제로 쭉 나열한다. 앞의 리프가 실패하면 뒤는 실행되지 않는다.
        const sequence = {
            type: 'Sequence',
            children: [
                { type: trigger, params: skill.triggerParams || {} }
            ]
        };

        if (skill.targeting)
        {
            sequence.children.push(skill.targeting);
        }
        sequence.children.push(...bodyNodes);

        const roots = this._buildNodes(sequence, slots, bindings, where, usedSlots);
        if (roots.length !== 1)
        {
            this._fail(where, `루트 노드가 1개가 아닙니다 (${roots.length}개)`);
            return null;
        }

        for (const name of Object.keys(slots))
        {
            if (usedSlots.has(name) === false)
            {
                this._fail(where, `슬롯 '${name}' 을 채웠지만 모듈이 선언하지 않았습니다 (사용 가능: ${[...usedSlots].join(', ') || '없음'})`);
            }
        }

        const tree = this._assignIds(roots[0], { next: 1 });
        this._validateStructure(tree, where);

        const maxLevel = Math.max(1, Number(skill.maxLevel) || 1);

        return {
            id: id,
            name: skill.name || '',
            desc: skill.desc || '',
            maxLevel: maxLevel,
            visualPath: visual ? visual.id : '',
            methodPath: method ? method.id : '',
            tree: tree,
            // 레벨 곡선도 Visual 이 들고 있다 (옛 stat 이 합쳐졌다)
            levels: this._buildLevels(visual ? visual.data : null, maxLevel, where)
        };
    }
}

module.exports = { Composer };
