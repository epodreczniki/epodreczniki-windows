# Wołanie przykładowe:
#	powershell -File prepareinstall.ps1 -d Develop -ver 1.2.0.100 

param (
$ver = "1.0.0.6"
)

$configuration = "Release"

$filestocopy = @(
	'ePodrecznikiDesktop.exe',
	'ePodrecznikiDesktop.exe.config',
	'ICSharpCode.SharpZipLib.dll'
)


function Update-Version
{
	param([string]$filePath,
		[string]$ver)
		
	if(Test-Path $filePath)
	{
		$newVer = '$1"' + $ver + '$4'
		
	 	&(Join-Path (Split-Path $Env:VS120COMNTOOLS -parent)  "\IDE\TF.exe") checkout  $filePath
		(Get-Content $filePath) |  Foreach-Object {$_ -replace '(^\[assembly: Assembly(Version|FileVersion)\()("[0-9\.]+)("\)\]$)', $newVer} | Set-Content "$($filePath)"
	 	&(Join-Path (Split-Path $Env:VS120COMNTOOLS -parent)  "\IDE\TF.exe") checkin  $filePath /noprompt /comment:"Zmiana wersji na $($ver)"
	}
	else
	{
		Write-Host -ForegroundColor Red "Nie znaleziono pliku "  $filePath
	}
}


if($args[0] -eq '-h')
{
	Write-Host "Paarametry wywołania:"
	Write-Host "	-ver  - wersja, domyślnie 1.0.0.0"
	Break
}



# Rebuild całego projektu
Write-Host -ForegroundColor Yellow "Kompilowanie projektu..."

$msbuild = Join-Path $Env:windir "\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
$devenv = Join-Path (Split-Path $Env:VS140COMNTOOLS -parent) "\IDE\devenv.com"
$solution = "..\ePodrecznikiDesktop.sln"

#& $devenv $solution /rebuild $configuration
& $msbuild $solution  /p:Configuration=$configuration /p:Platform="Any CPU" /t:Rebuild   /p:DefineConstants="$configuration"


#usuwanie katalogu install
Remove-Item .\install\*.dll -Force
Remove-Item .\install\ePodrecznikiDesktop.* -Force



#kopiowanie DLLi do katalogu install
Write-Host -ForegroundColor Yellow "Kopiowanie plików wynikowach..."
foreach ($file in $filestocopy) {
	$filePath = "..\ePodrecznikiDesktop\bin\Release\$file"
	if(Test-Path $filePath)
	{
		Write-Host "Kopiowanie " $filePath
		Copy-Item $filePath .\install
	}
}
 


#podpisywanie 
Write-Host -ForegroundColor Yellow "Podpisywanie certyfikatem..."
$signtool = Join-Path ${Env:ProgramFiles(x86)} "\Microsoft SDKs\Windows\v7.1A\Bin\signtool.exe"
& $signtool sign /v /i thawte -t http://timestamp.verisign.com/scripts/timstamp.dll ./install/ePodrecznikiDesktop.exe 




#trzoenie instalera
Write-Host -ForegroundColor Yellow "Tworzenie instalki..."
$innosetup = Join-Path ${Env:ProgramFiles(x86)} "\Inno Setup 5\ISCC.exe"


& $innosetup "Install.iss" "/dWersja=$ver"


Write-Host -ForegroundColor Yellow "Podpisanie instalki..."

$prodecedfile = ".\output\EP-$ver.exe"

& $signtool sign /v /i thawte -t http://timestamp.verisign.com/scripts/timstamp.dll $prodecedfile





