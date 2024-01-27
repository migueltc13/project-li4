#!/bin/sh

sqlcmd \
    -C \
    -S "betterfinds.pt" \
    -U "sa" \
    -P "LI4passwd2024" \
    -Q "RESTORE DATABASE master FROM DISK='/root/project-li4/sql/backup/database.bak' WITH REPLACE"

