#!/bin/bash
prompt=$(cat "orchestration/prompts/COORDINATOR.txt")
exec claude "$prompt"
