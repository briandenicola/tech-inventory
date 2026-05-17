#!/usr/bin/env bash
set -e
echo "📝 Update tasks.md, SESSION-NOTES.md, .copilot-state.md, then commit."
${EDITOR:-vi} SESSION-NOTES.md
git add -A
git commit -m "chore: session handoff" || echo "Nothing to commit"
