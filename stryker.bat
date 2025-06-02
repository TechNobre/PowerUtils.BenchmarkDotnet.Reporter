dotnet tool restore
dotnet restore
dotnet build --no-restore
dotnet stryker -tp tests/PowerUtils.BenchmarkDotnet.Reporter.Tests/PowerUtils.BenchmarkDotnet.Reporter.Tests.csproj -p PowerUtils.BenchmarkDotnet.Reporter.csproj --reporter json --reporter cleartext --reporter html -o
