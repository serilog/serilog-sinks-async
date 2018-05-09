echo "bench: Benchmarking started"

Push-Location $PSScriptRoot

& dotnet restore --no-cache

foreach ($test in ls test/*.PerformanceTests) {
    Push-Location $test

	echo "bench: Benchmarking project in $test"

    & dotnet test -c Release --framework net46
    if($LASTEXITCODE -ne 0) { exit 3 }

    Pop-Location
}

Pop-Location
