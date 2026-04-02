#!/bin/bash
# Set terminal title
echo -ne "\033]0;COORDINATOR\007"
prompt=$(cat "orchestration/prompts/COORDINATOR.txt")
exec claude -n "COORDINATOR" "$prompt"
