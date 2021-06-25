Get-ChildItem "$PSScriptRoot/nupkg" | Remove-Item
dotnet pack --configuration Release

Get-ChildItem "$PSScriptRoot/nupkg" | ForEach-Object {
	dotnet nuget push $_.FullName --source https://api.nuget.org/v3/index.json --api-key $env:Nuget_API_KEY_Personal
}