# -*- coding: utf-8 -*-
# 번역 원문 표 생성기 (com.translation.unity 의 게임 DB 쪽)
#
# - LocalizationData.json : 코드의 Loc.T("키", "한국어") · Loc.F("키", "한국어 {0}", ...) · LocLabel.Attach(x, "키", "한국어") · new Hero(n, "이름") 을 모은 UI 원문 표
# - ConstellationText.json : 성좌 그래프(ConstellationGraph.json)의 노드 이름 · 설명 (그래프 JSON 은 배열이 둘이라 번역 SDK 가 직접 못 읽는다)
# 둘 다 Assets/Resources/GameData 에 쓴다 — Translation Settings 의 게임 DB 폴더. 파츠 · 옵션 · 시너지 · 맵 표는 export.js 가 이미 쓴다
# 같은 키에 다른 한국어가 있으면 멈춘다 (한 키 = 한 원문)
#
# 사용: python gen_loc_source.py   → 이어서 에디터 태스크 locsync

import io, json, os, re, sys
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
SCRIPTS = os.path.join(ROOT, r"Assets\Scripts")
OUT = os.path.join(ROOT, r"Assets\Resources\GameData")
GRAPH = os.path.join(OUT, "ConstellationGraph.json")

STR = r'"((?:[^"\\]|\\.)*)"'
PATTERNS = [
    re.compile(r'Loc\.T\(\s*' + STR + r'\s*,\s*' + STR),
    re.compile(r'Loc\.F\(\s*' + STR + r'\s*,\s*' + STR),
    re.compile(r'LocLabel\.Attach\([^,]+,\s*' + STR + r'\s*,\s*' + STR),
]


def unescape(s):
    return s.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def scan():
    keys = {}
    where = {}
    for d, _, files in os.walk(SCRIPTS):
        if "GoogleSheetDataLoader" in d:
            continue
        for f in files:
            if not f.endswith(".cs"):
                continue
            p = os.path.join(d, f)
            # 주석 줄은 뺀다 (설명용 예시 Loc.T("키", "한국어") 가 원문으로 들어간다)
            lines = io.open(p, encoding="utf-8-sig").read().splitlines()
            text = "\n".join(l for l in lines if l.strip().startswith("//") is False)
            for pat in PATTERNS:
                for m in pat.finditer(text):
                    k, ko = m.group(1), unescape(m.group(2))
                    if k in keys and keys[k] != ko:
                        raise SystemExit("키 %s 의 원문이 둘이다: %r (%s) / %r (%s)" % (k, keys[k], where[k], ko, f))
                    keys[k] = ko
                    where[k] = f
    # 캐릭터 이름: Hero.Name 이 Loc.T("Hero_" + Index, 한국어) 로 키를 만든다 — 선언 줄에서 읽는다
    run = io.open(os.path.join(SCRIPTS, "Common", "RunSelection.cs"), encoding="utf-8-sig").read()
    for m in re.finditer(r'new Hero\(\s*(\d+)\s*,\s*' + STR, run):
        keys["Hero_" + m.group(1)] = unescape(m.group(2))
    return keys


def main():
    keys = scan()
    rows = [{"Key": k, "Korean": keys[k]} for k in sorted(keys)]
    io.open(os.path.join(OUT, "LocalizationData.json"), "w", encoding="utf-8").write(json.dumps(rows, ensure_ascii=False, indent=1))

    g = json.load(io.open(GRAPH, encoding="utf-8-sig"))
    nodes = [{"id": n["id"], "name": n.get("name", ""), "desc": n.get("desc", "")} for n in g["nodes"]]
    io.open(os.path.join(OUT, "ConstellationText.json"), "w", encoding="utf-8").write(json.dumps(nodes, ensure_ascii=False, indent=1))
    print("UI 원문 %d키 · 성좌 노드 %d" % (len(rows), len(nodes)))


main()
