# Runbook: Backup & Restore

## Backup (automated, nightly)
1. SQL backup sidecar runs at 02:00 local
2. Output to `/backups` volume
3. Off-host copy via rsync to NAS

## Restore Drill (quarterly)
1. Spin up fresh stack
2. Restore latest backup into new DB
3. Smoke test via API
4. Document outcome in `audits/`
