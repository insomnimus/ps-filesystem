namespace Filesystem;

[Cmdlet(VerbsCommon.Get, "LastItem", DefaultParameterSetName = "creation")]
[OutputType(typeof(FileSystemInfo))]
public class GetLastItem: Cmd {
	[Parameter(
		Position = 0,
		ValueFromPipeline = true,
		HelpMessage = "Path or glob pattern to items"
	)]
	[SupportsWildcards()]
	public object[] Path { get; set; } = ["*"];

	[Parameter(ParameterSetName = "creation", HelpMessage = "Get most recently created items")]
	public SwitchParameter Creation { get; set; }
	[Parameter(ParameterSetName = "modification", HelpMessage = "Get most recently modified items")]
	public SwitchParameter Modification { get; set; }
	[Parameter(ParameterSetName = "access", HelpMessage = "Get most recently accessed items")]
	public SwitchParameter Access { get; set; }

	[Parameter(HelpMessage = "Only include plain files")]
	public SwitchParameter File { get; set; }
	[Parameter(HelpMessage = "Only include directories")]
	public SwitchParameter Directory { get; set; }

	[Parameter(HelpMessage = "Return at most N items")]
	public ulong N { get; set; } = 1;
	[Parameter(HelpMessage = "Include hidden items")]
	public SwitchParameter Force { get; set; }

	private List<FileSystemInfo> files = new();

	protected override void BeginProcessing() {
		if (!this.File && !this.Directory) {
			this.File = this.Directory = true;
		}
	}

	protected override void ProcessRecord() {
		var fs = this.InvokeProvider.Item.Get(this.Path.Where(x => x is not null).Select(x => x.ToString()).ToArray(), this.Force, false);
		foreach (var x in fs) {
			if (x.BaseObject is FileSystemInfo info) {
				if ((this.File && this.Directory) || (this.File && info is FileInfo) || (this.Directory && info is DirectoryInfo)) {
					this.files.Add(info);
				}
			}
		}
	}

	protected override void EndProcessing() {
		this.files.Sort((a, b) => {
			if (this.Modification) return b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc);
			else if (this.Access) return b.LastAccessTimeUtc.CompareTo(a.LastAccessTimeUtc);
			else return b.CreationTimeUtc.CompareTo(a.CreationTimeUtc);
		});

		for (var i = 0; i < this.files.Count; i++) {
			if (this.N > 0 && (ulong)i >= N) {
				break;
			}
			WriteObject(this.files[i]);
		}
	}
}
