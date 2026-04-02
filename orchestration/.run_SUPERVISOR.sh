#!/bin/bash
prompt=$(cat "orchestration/prompts/SUPERVISOR.txt")
exec claude "$prompt"
