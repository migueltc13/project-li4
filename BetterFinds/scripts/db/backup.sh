#!/bin/sh

sqlcmd \
    -C \
    -S "betterfinds.pt" \
    -U "sa" \
    -P "LI4passwd2024" \
    -Q "BACKUP DATABASE master TO DISK='/tmp/database.bak'"

cp /tmp/database.bak /root/project-li4/sql/backup/database.bak

