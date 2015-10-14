param (
	[string]$configuration = "Release"
)

Set-Alias msbuild C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe;

$projectName = "Nop.Plugin.Shipping.SeeSharpShipUsps";
$projectPath = ".\$projectName\$projectName.csproj"; 
msbuild $projectPath /t:"Clean;Build" /p:Configuration=$configuration /v:m

Remove-Item alias:\msbuild;