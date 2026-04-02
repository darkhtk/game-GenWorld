#!/bin/bash
# Set terminal title
echo -ne "\033]0;SUPERVISOR\007"
prompt=$(cat "orchestration/prompts/SUPERVISOR.txt")
exec claude -n "SUPERVISOR" "$prompt"
