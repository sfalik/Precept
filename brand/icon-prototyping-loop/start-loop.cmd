@echo off
cd /d "%~dp0"
pwsh -NoProfile -NoLogo -Command "$prompt = Get-Content -Raw '.\overnight-prompt.txt'; copilot -i $prompt --autopilot --no-ask-user --model claude-sonnet-4.6 --effort medium --disallow-temp-dir --disable-builtin-mcps --available-tools=view --available-tools=glob --available-tools=create --available-tools=edit"
