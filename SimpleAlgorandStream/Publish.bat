echo off
set platforms=win-x64 linux-x64 osx-x64 win-arm64 linux-arm64 osx-arm64


for %%p in (%platforms%) do (
   echo Publishing for %%p...
   dotnet publish ./SimpleAlgorandStream.csproj -c Release -r %%p  --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true 
)

echo Publishing complete.
