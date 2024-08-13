using System.Management.Automation;

[Cmdlet(VerbsCommon.Find, "Item", DefaultParameterSetName = "script")]
[OutputType(typeof(FileSystemInfo))]
public class FindItem: Cmd {
	[Parameter(
		Mandatory = true,
		Position = 0,
		ParameterSetName = "glob",
		HelpMessage = "Find items with names that match the wildcard pattern"
	)]
	[SupportsWildcards()]
	public string[] Pattern { get; set; }

	[Parameter(
		Mandatory = true,
		Position = 0,
		ParameterSetName = "script",
		HelpMessage = "Use a script block to find items"
	)]
	public ScriptBlock ScriptBlock { get; set; }

	[Parameter(HelpMessage = "The directory to search in")]
	public string Root { get; set; } = ".";

	[Parameter(HelpMessage = "Maximum number of items to yield")]
	public ulong N { get; set; } = 1;
	[Parameter(HelpMessage = "Do not exit early, return all items that match")]
	public SwitchParameter All { get; set; }
	[Parameter(HelpMessage = "Find items in parent / ancestor directories")]
	public SwitchParameter Up { get; set; }

	[Parameter(HelpMessage = "Find directories only")]
	public SwitchParameter Directory { get; set; }
	[Parameter(HelpMessage = "Find plain files only")]
	public SwitchParameter File { get; set; }
	[Parameter(HelpMessage = "Find symbolic links and NTFS junctions")]
	public SwitchParameter Link { get; set; }
	[Parameter(HelpMessage = "Ignore items that are symbolic links or NTFS junctions")]
	public SwitchParameter NotLink { get; set; }

	private static ScriptBlock Truthy = System.Management.Automation.ScriptBlock.Create(@"$args[0] -as [bool]");
	private ulong progress = 0;
	private List<WildcardPattern> globs = new();

	protected override void BeginProcessing() {
		if (!this.Directory && !this.File) {
			this.File = this.Directory = true;
		}

		if (this.All) {
			this.N = ulong.MaxValue;
		}

		if (this.ParameterSetName == "glob") {
			this.globs = new List<WildcardPattern>(this.Pattern.Length);
			foreach (var s in this.Pattern) {
				this.globs.Add(new WildcardPattern(s, WildcardOptions.Compiled | WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase));
			}
		}
	}

	protected override void ProcessRecord() {
		if (this.progress >= this.N) return;

		var item = this.InvokeProvider.Item.Get([this.Root], true, true)[0];
		DirectoryInfo dir;
		if (item.BaseObject is DirectoryInfo d) {
			dir = d;
		} else {
			return;
		}

		if (this.Up) {
			this.findUp(dir);
			return;
		}

		var opts = new EnumerationOptions() {
			IgnoreInaccessible = true,
			RecurseSubdirectories = true,
			ReturnSpecialDirectories = false,
		};

		// This is about 10% faster than the equivalent Fs.Walk
		foreach (var x in dir.EnumerateFileSystemInfos("*", opts)) {
			this.CheckCancel();
			if (this.isMatch(x)) {
				WriteObject(x);
				this.progress++;
				if (this.progress >= this.N) {
					return;
				}
			}
		}
	}

	protected override void EndProcessing() { }

	private void findUp(DirectoryInfo dir) {
		var opts = new EnumerationOptions() {
			IgnoreInaccessible = true,
			RecurseSubdirectories = true,
			ReturnSpecialDirectories = false,
		};

		if (!this.Directory) {
			opts.AttributesToSkip |= FileAttributes.Directory;
		}

		while (dir is not null) {
			foreach (var x in dir.EnumerateFileSystemInfos("*", opts)) {
				this.CheckCancel();

				if (!this.File && !x.Attributes.HasFlag(FileAttributes.Directory)) {
					continue;
				}

				if (this.isMatch(x)) {
					WriteObject(x);
					this.progress++;
					if (this.progress >= this.N) {
						return;
					}
				}
			}

			try {
				dir = dir.Parent;
			} catch (Exception) {
				return;
			}
		}
	}

	private bool isMatch(FileSystemInfo x) {
		if (
	(!this.Directory && x.Attributes.HasFlag(FileAttributes.Directory)) ||
	(this.Link && !x.Attributes.HasFlag(FileAttributes.ReparsePoint)) ||
	(this.NotLink && x.Attributes.HasFlag(FileAttributes.ReparsePoint)) ||
(x is FileInfo && !this.File)
) {
			return false;
		}

		if (this.globs.Count != 0) {
			return this.globs.Any(g => g.IsMatch(x.Name));
		}
		// return this.ScriptBlock(x);

		System.Collections.IList input = new FileSystemInfo[] { x };
		this.SessionState.PSVariable.Set("_", x);
		var res = this.InvokeCommand.InvokeScript(false, this.ScriptBlock, input, [x]);
		return res.Count > 0 && (bool)Truthy.Invoke([res[0]])[0].BaseObject;
	}
}
