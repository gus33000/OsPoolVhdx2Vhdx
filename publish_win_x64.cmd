@echo off

msbuild /m /t:restore,ospoolvhdx2vhdx:publish /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:PublishDir="%CD%\publish\artifacts\win-x64\CLI" /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Configuration=Release OsPoolVhdx2Vhdx.sln