dotnet pack --configuration Release
dotnet nuget push .\nupkg\*.nupkg --source https://api.nuget.org/v3/index.json --api-key $env:Nuget_API_KEY_Personal