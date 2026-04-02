#!/bin/bash
prompt=$(cat "orchestration/prompts/CLIENT.txt")
exec claude "$prompt"
