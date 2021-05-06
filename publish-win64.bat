dotnet publish spa-prerenderer.csproj -c Release -o ./publish/win-x64 --self-contained -r win-x64
rmdir /s /q "./publish/win-x64/cache"
rmdir /s /q "./publish/win-x64/wwwroot"
mkdir "./publish/win-x64/cache"
mkdir "./publish/win-x64/wwwroot"