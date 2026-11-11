dotnet tool restore
dotnet restore
dotnet build --no-restore
pushd tests\PowerUtils.BenchmarkDotnet.Reporter.Tests
dotnet stryker -p PowerUtils.BenchmarkDotnet.Reporter.csproj --test-runner mtp --reporter json --reporter cleartext --reporter html -o
popd
