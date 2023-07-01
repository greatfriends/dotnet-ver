## dotnet-ver
.NET Core Project Version Tool

## How to install
```
% dotnet tool install dotnet-ver --global
```

## How to use

![Help screen](<help-screen.png>)

## How to update to nuget.org

```
% dotnet pack --configuration Release --output ./nupkg
% dotnet nuget push ./nupkg/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```
Assume that we saved the nuget API Key in the `NUGET_API_KEY` environment variable.
