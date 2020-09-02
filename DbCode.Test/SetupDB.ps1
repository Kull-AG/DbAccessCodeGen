$server = "(LocalDB)\MSSQLLocalDB"
$db = "CodeGenTestDb"
$shouldVNr = 2
$hasDb = ( & "sqlcmd" -E -S $server -d master -Q "SELECT name FROM sys.databases" ) | Where-Object { $_.Trim() -eq $db }
$createNewDb = $false
if ($hasDb) {
    Write-Host "Db exists"
    $outputVersion = & "sqlcmd" -S "(LocalDB)\MSSQLLocalDB" -d $db -Q "SELECT * FROM TestDbVersion"
    
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
    & "sqlcmd" -E -S  $server -d master -Q "DROP DATABASE $db"
    & "sqlcmd" -E -S  $server -d master -Q "CREATE DATABASE $db"
    & "sqlcmd" -i "$PSScriptRoot\sqlscript.sql" -S $server -d $db -v DbVersion=1
}
