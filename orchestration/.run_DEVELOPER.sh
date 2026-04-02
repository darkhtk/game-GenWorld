#!/bin/bash
prompt=$(cat "orchestration/prompts/DEVELOPER.txt")
exec claude "$prompt"
