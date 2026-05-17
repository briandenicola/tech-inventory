# Copilot Instructions

## Always
- Read `.specify/memory/constitution.md` before any task
- Read `.copilot-state.md` at session start
- Quote spec sections you're implementing in commit messages
- Run the verify pipeline before declaring done
- Update `tasks.md`, `SESSION-NOTES.md`, `.copilot-state.md` at session end
- Cite the constitution by section when making design decisions

## Never
- Use `any` / `dynamic` / `@ts-ignore` without inline justification
- Commit secrets or `.env` files
- Skip tests or disable lint rules silently
- Paraphrase files from memory — re-read or quote
- Continue if context feels stale — request `/compact` or restart
- Invent APIs, package names, or file paths

## Context Discipline
- Prefer file paths + `grep`/`rg` over reading whole files
- Summarize tool output in ≤ 3 lines
- If > 50% context used, suggest `/compact` or session end
- One task = one PR = one commit
- One feature per session — never juggle multiple specs

## Session Handoff Protocol
START of session:
1. Read `.copilot-state.md`
2. Read last entry in `SESSION-NOTES.md`
3. Verify against current `tasks.md`
4. State next action and wait for confirmation

END of session:
1. Update `tasks.md` (check off completed items)
2. Append to `SESSION-NOTES.md` (done / next / gotchas)
3. Update `.copilot-state.md` with exact next action
4. Commit with `chore: session handoff`
