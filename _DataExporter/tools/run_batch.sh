#!/usr/bin/env bash
# 다중 시드 필멸 일괄 (TASKS B-1). 에디터가 떠 있어야 한다 (Temp/ 하네스)
# 사용: bash _DataExporter/tools/run_batch.sh "<시드 목록>" "<주인공 목록>" <맵> [초] [성장 판 수]
#   예: bash _DataExporter/tools/run_batch.sh "11 12 13 14 15" "1 2 3 4 5 6 7 8 9 10" 1
# 결과: Temp/runloop_results.tsv 에 판마다 한 줄 (RunLoopCheck.AppendResult). 시작 전에 지우지 않는다 — 이어 붙인다
cd "$(dirname "$0")/../.."
SEEDS="$1"; HEROES="$2"; MAP="${3:-1}"; SECS="${4:-820}"; GROWTH="${5:-0}"
for s in $SEEDS; do
  for h in $HEROES; do
    rm -f Temp/runloop.report
    printf "mortal:%s:%s:-1:%s:%s:%s" "$SECS" "$h" "$MAP" "$s" "$GROWTH" > Temp/runloop.request
    for i in $(seq 1 600); do [ -f Temp/runloop.report ] && break; sleep 3; done
    [ -f Temp/runloop.report ] || touch Temp/editor.stop
    echo "seed $s hero $h $(date +%H:%M) $(grep -a '속도' Temp/runloop.report)"
  done
done
