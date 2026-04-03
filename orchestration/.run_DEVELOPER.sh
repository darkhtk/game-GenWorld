#!/bin/bash
echo -ne "\033]0;DEVELOPER\007"
cd "/c/sourcetree/GENWorld"
CONFIG_FILE="orchestration/project.config.md"
PROMPT_FILE="orchestration/prompts/DEVELOPER.txt"

get_config_value() {
  local key="$1"
  [ -f "$CONFIG_FILE" ] || return 0
  grep -m1 "^-[[:space:]]*${key}:" "$CONFIG_FILE" | sed "s/^-[[:space:]]*${key}:[[:space:]]*//"
}

interval_to_seconds() {
  local raw
  raw=$(echo "$1" | tr -d '[:space:]' | tr '[:upper:]' '[:lower:]')
  case "$raw" in
    *h) echo $(( ${raw%h} * 3600 )) ;;
    *m) echo $(( ${raw%m} * 60 )) ;;
    *s) echo $(( ${raw%s} )) ;;
    *)  echo "$raw" ;;
  esac
}

INTERVAL_RAW=$(get_config_value "Loop interval")
[ -z "$INTERVAL_RAW" ] && INTERVAL_RAW="2m"
INTERVAL_SECONDS=$(interval_to_seconds "$INTERVAL_RAW")
case "$INTERVAL_SECONDS" in
  ''|*[!0-9]*) INTERVAL_SECONDS=120 ;;
esac
if [ "$INTERVAL_SECONDS" -le 0 ] 2>/dev/null; then
  INTERVAL_SECONDS=120
fi

while true; do
  PROMPT=$(cat "$PROMPT_FILE")
  echo "=== [$(date '+%H:%M:%S')] DEVELOPER loop start (interval: $INTERVAL_RAW) ==="
  claude -p "$PROMPT"
  echo ""
  sleep "$INTERVAL_SECONDS"
done
