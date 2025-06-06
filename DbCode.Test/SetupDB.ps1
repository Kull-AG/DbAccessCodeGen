﻿docker-compose up -d  > $null #remove status entries

Start-Sleep -Seconds 20 

$server = "127.0.0.1"
$user = "sa"
$pw = "abcDEF123#"
$db = "CodeGenTestDb"
$shouldVNr = 14
$hasDb = ( & "sqlcmd" -S $server -d master -Q "SELECT name FROM sys.databases" -U $user -P $pw ) | Where-Object { $_.Trim() -eq $db }

Write-Host "has database $hasDb"

$createNewDb = $false
if ($hasDb) {
    Write-Host "Db exists"
    $outputVersion = & "sqlcmd" -S $server -U $user -P $pw -d $db -Q "SELECT * FROM TestDbVersion"
    
    if($outputVersion -like '*invalid object name*')
    {
        $createNewDb = $true
        Write-Host "Will drop and recreated db"
    }
    else {
        $currentVNr = [System.Int32]::Parse(($outputVersion)[2].Trim())
        if($currentVNr -ne $shouldVNr) {
            Write-Host "Will drop and recreated db. Old VNr: $currentVNr, new Nr: $shouldVNr"
            $createNewDb=$true
        }
    }
}
else {
    $createNewDb=$true
}
if($createNewDb) {
    Write-Host "recreate db"
    & "sqlcmd" -S  $server -U $user -P $pw -d master -Q "DROP DATABASE IF EXISTS $db"
    & "sqlcmd" -S  $server -U $user -P $pw -d master -Q "CREATE DATABASE $db"
    & "sqlcmd" -i "$PSScriptRoot\sqlscript.sql" -S $server -U $user -P $pw -d $db -v DbVersion=1
}

#destroy the container
#docker-compose down