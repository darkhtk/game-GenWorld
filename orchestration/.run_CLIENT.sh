#!/bin/bash
# Set terminal title
echo -ne "\033]0;CLIENT\007"
prompt=$(cat "orchestration/prompts/CLIENT.txt")
exec claude -n "CLIENT" "$prompt"
