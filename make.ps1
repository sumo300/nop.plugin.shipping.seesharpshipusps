param (
	[string]$configuration = "Release"
)

Set-Alias msbuild "C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe";

$projectName = "Nop.Plugin.Shipping.SeeSharpShipUsps";
$projectPath = ".\$projectName\$projectName.csproj"; 
msbuild $projectPath /t:"Clean;Build" /p:Configuration=$configuration /v:m

Remove-Item alias:\msbuild;