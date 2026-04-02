#!/bin/bash
# Set terminal title
echo -ne "\033]0;DEVELOPER\007"
prompt=$(cat "orchestration/prompts/DEVELOPER.txt")
exec claude -n "DEVELOPER" "$prompt"
