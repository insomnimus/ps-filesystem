@{
	ModuleVersion = "0.1.0"
	RootModule = "Filesystem.psm1"
	Description = "Cmdlets for various filesystem operations"
	Author = "Taylan Gökkaya"
	GUID = "7e82623d-71d9-41b5-a1d0-e38403200a2c"
	PowerShellVersion = "5.1"

	# FormatsToProcess = @()
	NestedModules = @("Filesystem.dll")
	FunctionsToExport = @("Select-Item")
	CmdletsToExport = @(
		"Expand-Directory"
		"Find-Item"
		"Format-Path"
		"Get-RelativePath"
		"Measure-DiskUsage"
		"Measure-Size"
		"Test-Item"
	)
	AliasesToExport = @("fd", "fdi", "normalize", "relpath", "sz", "unnest")
	VariablesToExport = @()

	HelpInfoURI = "https://github.com/insomnimus/ps-filesystem"
	PrivateData = @{
		PSData = @{
			# Tags = @()
			# LicenseUri = ""
			ProjectUri = "https://github.com/insomnimus/ps-filesystem"
		}
	}
}
