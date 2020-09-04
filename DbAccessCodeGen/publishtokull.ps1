Remove-Item -Recurse -Force nupkg
dotnet pack
$file = (Get-ChildItem nupkg)[0]
$file
dotnet nuget push --source "https://pkgs.dev.azure.com/kull-ag-aarau/_packaging/kull-ag-aarau/nuget/v3/index.json" nupkg/($file.Name) --api-key az