Set-Alias fd Filesystem\Find-Item
Set-Alias fdi Filesystem\Find-Item
Set-Alias normalize Filesystem\Format-Path
Set-Alias relpath Filesystem\Get-RelativePath
Set-Alias rename Filesystem\Rename-Item
Set-Alias sz Filesystem\Measure-Size
Set-Alias unnest Filesystem\Expand-Directory

function Select-Item {
	[CmdletBinding()]
	[OutputType([IO.FileSystemInfo])]
	param (
		[Parameter(Mandatory, Position = 0, ValueFromPipeline, HelpMessage = "Path to the item")]
		[object[]] $Path,

		[Parameter(HelpMessage = "Item type")]
		[TestItemType] $Type = [TestItemType]::Any,
		[Parameter(HelpMessage = "Select symbolic links or NTFS junctions")]
		[switch] $Link,
		[Parameter(HelpMessage = "Select empty files or directories")]
		[switch] $Empty
	)

	begin {}

	process {
		foreach($p in $path) {
			if($null -eq $p) {
				continue
			}

			$item = Get-Item -ea stop -lp:$p
			if(Filesystem\Test-Item `
					-Path:$item.FullName`
					-Type:$Type `
					-Empty:$Empty `
					-Link:$Link
			) {
				$item
			}
		}
	}

	end {}
}

function _Get-LastItem {
	[CmdletBinding(DefaultParameterSetName = "creation")]
	[OutputType([IO.FileSystemInfo])]
	param (
		[Parameter(Position = 0, ValueFromPipeline, HelpMessage = "Path to the items")]
		[SupportsWildcards()]
		[object[]] $Path = "*",

		[Parameter(HelpMessage = "Include only plain files")]
		[switch] $File,
		[Parameter(HelpMessage = "Include only directories")]
		[switch] $Directory,
		[Parameter(HelpMessage = "Get at most N items")]
		[uint] $N = 1,

		[Parameter(ParameterSetName = "creation", HelpMessage = "Get most recent by creation time")]
		[switch] $Creation,
		[Parameter(ParameterSetName = "modification", HelpMessage = "Get most recent by modification time")]
		[switch] $Modification,
		[Parameter(ParameterSetName = "access", HelpMessage = "Get most recent by last access time")]
		[switch] $Access
	)

	begin {
		$files = [Collections.Generic.List[IO.FileSystemInfo]]::new()
		if(!$file -and !$Directory) {
			$File = $Directory = $true
		}
	}

	process {
		foreach($f in Get-Item -ea stop -Path:$Path) {
			if(($file -and $directory) -or ($file -and $f -is [IO.FileInfo]) -or ($Directory -and $f -is [IO.DirectoryInfo])) {
				[void] $files.Add($f)
			}
		}
	}

	end {
		$sortFunc = {
			if($Modification) { $_.LastWriteTimeUtc }
			elseif($Access) { $_.LastAccessTimeUtc }
			else { $_.CreationTimeUtc }
		}

		if($N -eq 0) {
			$files | Sort-Object -Descending $sortFunc
		} else {
			$files | Sort-Object -Descending $sortFunc | Select-Object -First $N
		}
	}
}
