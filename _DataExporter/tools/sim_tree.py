# -*- coding: utf-8 -*-
# 스킬 트리(성좌) 구매 곡선 시뮬레이터 (TASKS B-3 · 진행 1~2일)
# RunLoopCheck.ApplyGrowth 와 같은 규칙: 판당 DNA 31 + 앞 챕터 첫 클리어 30, 닿는 노드 중 가장 싼 것부터
# 노드 비용 = 기본(종류) × 고리 성장^고리 (CommonConfig.Constellation · ConstellationGenerator.CostOf 와 같은 식)
# 출력: 판 수별 특성 단계 비율(트리 힘의 대용) — 65판까지 고르게 오르는 비용 상수를 고른다
#
# 사용: python sim_tree.py [small level notable skill gate keystone growth]

import json, os, sys, heapq
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
G = json.load(open(os.path.join(ROOT, r"Assets\Resources\GameData\ConstellationGraph.json"), encoding="utf-8"))
CHAPTER_RUNS = [2, 5, 9, 14, 20, 27, 35, 44, 54, 65]
DNA_PER_RUN, FIRST_CLEAR = 31, 30
LEVEL_GROWTH = 1.25


def cost_of(base, ring, growth):
    return max(1, int(base * growth ** ring + 0.5))


def simulate(bases, growth, g):
    nodes = {n["id"]: dict(n) for n in G["nodes"]}
    for n in nodes.values():
        if n["type"] != "Core":
            n["cost"] = cost_of(bases[n["type"]], n["ring"], growth)
    nb = {i: [] for i in nodes}
    for a, b in G["edges"]:
        nb[a].append(b)
        nb[b].append(a)
    cleared = sum(1 for r in CHAPTER_RUNS if r < g)
    dna = g * DNA_PER_RUN + cleared * FIRST_CLEAR
    level = {i: (1 if n["type"] == "Core" else 0) for i, n in nodes.items()}

    def next_cost(n):
        if n["type"] == "Level":
            return max(1, int(n["cost"] * LEVEL_GROWTH ** level[n["id"]] + 0.5))
        return n["cost"]

    def reach(n):
        if n["type"] == "Core" or level[n["id"]] >= n["maxLevel"]:
            return False
        if n["type"] == "Gate" and cleared < max(1, n["needClear"]):
            return False
        return level[n["id"]] > 0 or any(level[j] > 0 for j in nb[n["id"]])

    while True:
        cands = [(next_cost(n), n["id"]) for n in nodes.values() if reach(n)]
        if not cands:
            break
        c, i = min(cands)
        if c > dna:
            break
        dna -= c
        level[i] += 1
    trait_total = sum(n["maxLevel"] for n in nodes.values() if n["trait"] != "None")
    trait_got = sum(level[i] for i, n in nodes.items() if n["trait"] != "None")
    total = sum((sum(max(1, int(n["cost"] * LEVEL_GROWTH ** l + 0.5)) for l in range(n["maxLevel"])) if n["type"] == "Level" else n["cost"])
                for n in nodes.values())
    return trait_got / trait_total, total


def curve(bases, growth):
    pts = [5, 10, 20, 30, 40, 50, 60, 65]
    out = []
    for g in pts:
        f, total = simulate(bases, growth, g)
        out.append("%d판 %3.0f%%" % (g, f * 100))
    return total, "  ".join(out)


if __name__ == "__main__":
    cur = {"Small": 1, "Level": 2, "Notable": 5, "Skill": 6, "Gate": 4, "Keystone": 20, "Core": 0}
    if len(sys.argv) >= 8:
        v = [float(x) for x in sys.argv[1:8]]
        cur = {"Small": v[0], "Level": v[1], "Notable": v[2], "Skill": v[3], "Gate": v[4], "Keystone": v[5], "Core": 0}
        growth = v[6]
    else:
        growth = 1.4
    total, line = curve(cur, growth)
    print("총 DNA %d | %s" % (total, line))


# ---------------------------------------------------------------- 힘 배율 (맵 난이도 보정용)
# SkillModifier 규칙: 피해 = 1 + 합 · 쿨 = max(0.2, 1 - 합) · 범위 · 지속 = 1 + 합. 계열 한정 노드는 0.35 로 친다(쓰는 계열이 일부라)
# 스탯: SaveData.StatCost = 100 + 70L + 5L², 판당 사료 1,500, 싼 것부터. 공격 +2%/Lv · 체력 +3%/Lv
KIBBLE_PER_RUN = 1500
FAMILY_WEIGHT = 0.35


def stat_levels(g):
    kib = g * KIBBLE_PER_RUN
    lv = {"atk": 0, "hp": 0, "spd": 0}
    while True:
        k = min((s for s in lv if lv[s] < 20), key=lambda s: 100 + 70 * lv[s] + 5 * lv[s] ** 2, default=None)
        if k is None:
            break
        c = 100 + 70 * lv[k] + 5 * lv[k] ** 2
        if c > kib:
            break
        kib -= c
        lv[k] += 1
    return lv


def traits_at(bases, growth, g):
    nodes = {n["id"]: dict(n) for n in G["nodes"]}
    for n in nodes.values():
        if n["type"] != "Core":
            n["cost"] = cost_of(bases[n["type"]], n["ring"], growth)
    nb = {i: [] for i in nodes}
    for a, b in G["edges"]:
        nb[a].append(b)
        nb[b].append(a)
    cleared = sum(1 for r in CHAPTER_RUNS if r < g)
    dna = g * DNA_PER_RUN + cleared * FIRST_CLEAR
    level = {i: (1 if n["type"] == "Core" else 0) for i, n in nodes.items()}

    def nc(n):
        return max(1, int(n["cost"] * LEVEL_GROWTH ** level[n["id"]] + 0.5)) if n["type"] == "Level" else n["cost"]

    def reach(n):
        if n["type"] == "Core" or level[n["id"]] >= n["maxLevel"]:
            return False
        if n["type"] == "Gate" and cleared < max(1, n["needClear"]):
            return False
        return level[n["id"]] > 0 or any(level[j] > 0 for j in nb[n["id"]])

    while True:
        cands = [(nc(n), n["id"]) for n in nodes.values() if reach(n)]
        if not cands:
            break
        c, i = min(cands)
        if c > dna:
            break
        dna -= c
        level[i] += 1
    t = {}
    for i, n in nodes.items():
        if n["trait"] == "None" or level[i] == 0:
            continue
        w = FAMILY_WEIGHT if n["familyScoped"] else 1.0
        t[n["trait"]] = t.get(n["trait"], 0) + n["value"] * level[i] * w
    return t


def power(g, bases=None, growth=1.7):
    bases = bases or {"Small": 1, "Level": 2, "Notable": 5, "Skill": 6, "Gate": 4, "Keystone": 20, "Core": 0}
    s = stat_levels(g)
    t = traits_at(bases, growth, g)
    crit = 1 + min(1.0, 0.05 + t.get("CritChance", 0)) * (0.5 + t.get("CritDamage", 0)) - 0.05 * 0.5
    off = (1 + 0.02 * s["atk"]) * (1 + t.get("Damage", 0)) / max(0.2, 1 - t.get("Cooldown", 0)) * crit \
        * (1 + 0.5 * t.get("Area", 0)) * (1 + 0.3 * t.get("Count", 0)) * (1 + 0.3 * t.get("Duration", 0))
    de = (1 + 0.03 * s["hp"]) * (1 + t.get("MaxHp", 0))
    return off, de, s, t


def power_table():
    gs = [1, 4, 8, 13, 19, 26, 34, 43, 53, 64]
    o1, d1, _, _ = power(1)
    for m, g in enumerate(gs, start=1):
        o, d, s, t = power(g)
        print("맵 %2d g %2d  공격 x%.2f  생존 x%.2f  (스탯 %s, 피해 +%.0f%% 쿨 -%.0f%% 체력 +%.0f%%)" % (
            m, g, o / o1, d / d1, s, t.get("Damage", 0) * 100, t.get("Cooldown", 0) * 100, t.get("MaxHp", 0) * 100))
