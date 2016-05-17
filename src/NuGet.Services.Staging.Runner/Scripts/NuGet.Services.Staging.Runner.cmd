@echo OFF
	
cd bin

:Top
	echo "Starting job - #{Jobs.staging.runner.Title}"
	
	title #{Jobs.stats.collectazurecdnlogs.Title}

    start /w NuGet.Services.Staging.Runner.exe
	
	echo "Finished #{Jobs.staging.runner.Title}"

	goto Top