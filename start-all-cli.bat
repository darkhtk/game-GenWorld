@echo off
chcp 65001 >nul
REM GenWorld Multi-CLI Orchestration Launcher

"%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe" -w 0 ^
  new-tab -d "C:\sourcetree\GenWorld" --title "Director" -- claude "You are Director. Read docs/orchestration/roles/director.md and start monitoring. Check status/ and manage assignments/." ^
  ; new-tab -d "C:\sourcetree\GenWorld" --title "Dev-Backend" -- claude "You are Dev-Backend. Read docs/orchestration/roles/dev-backend.md, then check assignments/dev-backend.md and start working." ^
  ; new-tab -d "C:\sourcetree\GenWorld" --title "Dev-Frontend" -- claude "You are Dev-Frontend. Read docs/orchestration/roles/dev-frontend.md, then check assignments/dev-frontend.md and start working." ^
  ; new-tab -d "C:\sourcetree\GenWorld" --title "Asset-QA" -- claude "You are Asset/QA. Read docs/orchestration/roles/asset-qa.md, then check assignments/asset-qa.md and start working."
