cd src\DartTeamCityTestReporter
dotnet restore
dotnet publish --framework net451
cd ..\..
cpack