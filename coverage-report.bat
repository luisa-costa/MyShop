@ECHO OFF

for /d /r tests %%d in (TestResults) do @if exist "%%d" rd /s /q "%%d"

@if exist "%CD%\TestResults" rd /s /q "%CD%\TestResults"

REM Install tools if not present
dotnet tool install --global coverlet.console
dotnet tool install --global dotnet-reportgenerator-globaltool

REM Clean and build solution
dotnet restore MyShop.slnx
dotnet build MyShop.slnx --configuration Release --no-restore

REM Run tests with coverage
dotnet test MyShop.slnx --no-restore --verbosity normal ^
/p:CollectCoverage=true ^
/p:CoverletOutputFormat=cobertura ^
/p:CoverletOutput=./TestResults/coverage.cobertura.xml ^
/p:Exclude="[*]*.Migrations.*"

REM Generate coverage report
reportgenerator ^
-reports:"./tests/**/TestResults/**/coverage.cobertura.xml" ^
-targetdir:"./TestResults/CoverageReport" ^
-reporttypes:Html

REM Removing temporary files
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

echo.
echo Coverage report generated at TestResults/CoverageReport/index.html

start firefox %CD%\TestResults\CoverageReport\index.html

pause