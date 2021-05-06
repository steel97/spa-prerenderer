dotnet publish spa-prerenderer.csproj -c Release -o ./publish/lin-x64 --self-contained -r linux-x64
rmdir /s /q "./publish/lin-x64/cache"
rmdir /s /q "./publish/lin-x64/wwwroot"
mkdir "./publish/lin-x64/cache"
mkdir "./publish/lin-x64/wwwroot"