@echo off

:: set build exe file path
set buildExe="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" 

:: set properties 
set config="Release"
set publish=%1

:: check properties
if not "%publish%"=="local" if not "%publish%"=="nuget" if not "%publish%"=="none" (
	echo Invalid publish option. Use 'local', 'nuget' or 'none'. Example usage: 'build.bat local'.
	goto scriptExit
)

:: restore nuget
nuget restore 

:: build with local publish
if "%publish%"=="local" (
	%buildExe% Layer4Stack.csproj /p:Configuration=%config% /t:AfterBuild,Package,PublishLocal
)
:: build with nuget remote publish
if "%publish%"=="nuget" (
	%buildExe% Layer4Stack.csproj /p:Configuration=%config% /t:AfterBuild,Package,PublishNuGet
)
:: build without publish
if "%publish%"=="none" (
	%buildExe% Layer4Stack.csproj /p:Configuration=%config%
)

:: script exit
:scriptExit