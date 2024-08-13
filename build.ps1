#!/usr/bin/env -S pwsh -nologo -noprofile

trap {
	write-error -ea continue "error: $_"
	exit 1
}

dotnet publish -c release $PSScriptRoot
if($LastExitCode -ne 0) {
	throw "failed to build the project"
}

$ErrorActionPreference = "stop"

$p = join-path $PSScriptRoot bin/Release/netstandard2.1/publish
$dest = join-path $PSScriptRoot bin/Filesystem
remove-item -recurse -ea ignore -lp $dest
copy-item -recurse -lp $p, "$PSScriptRoot/Filesystem.psd1", "$PSScriptRoot/Filesystem.psm1" $dest

"success: built the module into $dest"
