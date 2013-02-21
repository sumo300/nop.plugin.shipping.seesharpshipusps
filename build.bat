@echo off

SETLOCAL

SET msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
SET projectName=Nop.Plugin.Shipping.SeeSharpShipUsps

%msbuild% .\%projectName%\%projectName%.csproj /t:Clean;Build /p:Configuration=Release /v:m