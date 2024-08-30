param([switch] $build)

if(!$build -and -not (test-path -lp "$PSScriptRoot/bin/Filesystem")) {
	$build = $true
}

if($build) {
	# dotnet build -c release $PSScriptRoot
	$null = & "$PSScriptRoot/build.ps1"
	if($LastExitCode -ne 0) {
		exit
	}
}

pwsh.exe -nologo -noe -c "
remove-alias sz
ipmo $PSScriptRoot/bin/Filesystem
sal dua measure-diskusage
# function fp { format-path -pretty @args }
sal fp format-path
"
