#!/usr/bin/env bash
# 맵 난이도 보정 일괄 (TASKS B-3). 맵 n 을 챕터 첫 클리어 목표 직전 성장으로 잰다
# 사용: bash _DataExporter/tools/cal_maps.sh "<맵 목록>" "<시드>" "<주인공>"
#   예: bash _DataExporter/tools/cal_maps.sh "1 2 3 4 5 6 7 8 9 10" "21 22" "1 6 7"
# 성장 = 볼트 기획서 진행 절의 챕터 첫 클리어 누적 판 - 1 (RunLoopCheck.ChapterRuns 와 같다)
cd "$(dirname "$0")/../.."
GROWTH=(0 1 4 8 13 19 26 34 43 53 64)
MAPS="$1"; SEEDS="$2"; HEROES="${3:-1 6 7}"
for m in $MAPS; do
  g=${GROWTH[$m]}
  for s in $SEEDS; do
    for h in $HEROES; do
      rm -f Temp/runloop.report
      printf "mortal:820:%s:-1:%s:%s:%s" "$h" "$m" "$s" "$g" > Temp/runloop.request
      for i in $(seq 1 600); do [ -f Temp/runloop.report ] && break; sleep 3; done
      [ -f Temp/runloop.report ] || touch Temp/editor.stop
      echo "map $m g $g seed $s hero $h $(date +%H:%M) $(grep -a '속도' Temp/runloop.report 2>/dev/null)"
    done
  done
done
