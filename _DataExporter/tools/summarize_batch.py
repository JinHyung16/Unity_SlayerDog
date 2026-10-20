# -*- coding: utf-8 -*-
# 다중 시드 일괄 결과 요약 (TASKS B-1 · B-2). 입력 Temp/runloop_results.tsv (RunLoopCheck.AppendResult)
# 기준은 볼트 SlayerDog 기획서 밸런싱 절: 화력 편차 1.5배 이하 · 클리어율 · 클리어 판 막판 최저 HP 30~60%
#
# 사용: python summarize_batch.py [tsv 경로]

import csv, os, sys, statistics
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\jinhyeong_project\Unity_SkillSystem_Module"
PATH = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Temp", "runloop_results.tsv")
NAMES = ["시바", "진돗개", "아키타", "차우차우", "퍼그", "셰퍼드", "푸들", "허스키", "비글", "닥스훈트"]

rows = list(csv.DictReader(open(PATH, encoding="utf-8-sig"), delimiter="\t"))
# 같은 (캐릭터, 맵, 시드) 는 결과가 같다(시드 고정) — 중복 줄은 하나만
seen, uniq = set(), []
for r in rows:
    k = (r["hero"], r["map"], r["seed"])
    if k not in seen:
        seen.add(k)
        uniq.append(r)
rows = uniq
by = {}
for r in rows:
    by.setdefault((int(r["map"]), int(r["hero"])), []).append(r)

print("| 맵 | 캐릭터 | 판 | 클리어 | 평균 생존(초) | 라운드5 처치 중앙 | 클리어 판 막판 최저 HP 중앙 |")
print("|---|---|---|---|---|---|---|")
k5_all = {}
for (m, h), rs in sorted(by.items()):
    n = len(rs)
    clear = sum(int(r["cleared"]) for r in rs)
    surv = statistics.mean(float(r["survived"]) for r in rs)
    k5 = [int(r["kills_r5"]) for r in rs if int(r["kills_r5"]) >= 0]
    k5m = statistics.median(k5) if k5 else -1
    k5_all.setdefault(m, {})[h] = k5m
    late = [float(r["late_min_hp"]) for r in rs if r["cleared"] == "1"]
    lm = "%.0f" % statistics.median(late) if late else "-"
    print("| %d | %s | %d | %d/%d (%.0f%%) | %.0f | %s | %s |" % (m, NAMES[h - 1] if h <= len(NAMES) else h, n, clear, n, clear * 100.0 / n, surv, k5m, lm))

for m, d in k5_all.items():
    vals = [v for v in d.values() if v > 0]
    if vals:
        print("\n맵 %d 라운드5 처치 편차: %d ~ %d = %.2f배 (목표 1.5 이하)" % (m, min(vals), max(vals), max(vals) / min(vals)))
    total = [r for r in rows if int(r["map"]) == m]
    print("맵 %d 전체 클리어율: %d/%d (%.0f%%)" % (m, sum(int(r["cleared"]) for r in total), len(total),
                                           sum(int(r["cleared"]) for r in total) * 100.0 / max(1, len(total))))
