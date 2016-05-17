@echo OFF
	
cd bin

:Top
	echo "Starting job - #{Jobs.staging.runner.Title}"
	
	title #{Jobs.staging.runner.Title}

    start /w NuGet.Services.Staging.Runner.exe  #{Jobs.staging.runner.Environment}
	
	echo "Finished #{Jobs.staging.runner.Title}"

	goto Top