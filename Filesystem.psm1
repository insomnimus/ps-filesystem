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
