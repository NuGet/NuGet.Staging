@echo OFF
	

:Top
	echo "Starting job - #{Jobs.NuGet.Services.Staging.Runner.Title}"
	
	title #{Jobs.NuGet.Services.Staging.Runner.Title}

    start /w NuGet.Services.Staging.Runner.exe  #{Jobs.NuGet.Services.Staging.Runner.Environment}
	
	echo "Finished #{Jobs.NuGet.Services.Staging.Runner.Title}"

	goto Top