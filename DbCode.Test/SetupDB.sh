#!/bin/bash

docker-compose up -d > /dev/null

sleep 20

server="127.0.0.1"
user="sa"
pw="abcDEF123#"
db="CodeGenTestDb"
shouldVNr=14
createNewDb=false

hasDb=$(sqlcmd -S "$server" -d master -U "$user" -P "$pw" -Q "SELECT name FROM sys.databases" -h -1 -W | grep -Fx "$db")

echo "has database: $hasDb"

if [ -n "$hasDb" ]; then
    echo "Db exists"
    outputVersion=$(sqlcmd -S "$server" -U "$user" -P "$pw" -d "$db" -Q "SELECT * FROM TestDbVersion" -h -1 -W 2>&1)

    if echo "$outputVersion" | grep -iq "invalid object name"; then
        echo "Will drop and recreate db"
        createNewDb=true
    else
        currentVNr=$(echo "$outputVersion" | sed -n '2p' | xargs)
        if [ "$currentVNr" != "$shouldVNr" ]; then
            echo "Will drop and recreate db. Old VNr: $currentVNr, new Nr: $shouldVNr"
            createNewDb=true
        fi
    fi
else
    createNewDb=true
fi

if [ "$createNewDb" = true ]; then
    echo "Recreate DB..."
    sqlcmd -S "$server" -U "$user" -P "$pw" -d master -Q "DROP DATABASE IF EXISTS [$db]"
    sqlcmd -S "$server" -U "$user" -P "$pw" -d master -Q "CREATE DATABASE [$db]"
    sqlcmd -i "$(dirname "$0")/sqlscript.sql" -S "$server" -U "$user" -P "$pw" -d "$db" -v DbVersion=1
fi

# docker-compose down
