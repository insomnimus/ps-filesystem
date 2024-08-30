namespace Filesystem;

public struct SizeInfo {
	public ByteSize Size { get; set; }
	public FileSystemInfo Item { get; set; }
}

[Cmdlet(
VerbsDiagnostic.Measure, "DiskUsage",
	DefaultParameterSetName = "wildcard"
)]
[OutputType(typeof(SizeInfo))]
public class MeasureDiskUsage: Cmd {
	[Parameter(
		Position = 0,
		ParameterSetName = "wildcard",
		HelpMessage = "Path to the item"
	)]
	[SupportsWildcards()]
	public object[] Path { get; set; } = ["."];

	[Parameter(
		Mandatory = true,
		ValueFromPipeline = true,
		ParameterSetName = "lp",
		HelpMessage = "The literal path to the item"
	)]
	[Alias("lp")]
	public object[] LiteralPath { get; set; }

	private List<FileSystemInfo> items = new List<FileSystemInfo>();
	private bool isLp = false;
	private int inputCount = 0;

	protected override void BeginProcessing() {
		this.isLp = this.ParameterSetName == "lp";
	}

	protected override void ProcessRecord() {
		this.inputCount += this.isLp ? this.LiteralPath.Length : this.Path.Length;
		var strings = (this.isLp ? this.LiteralPath : this.Path)
		.Where(x => x is not null)
		.Select(x => x.ToString()).ToArray();
		var newItems = this.InvokeProvider.Item.Get(strings, true, this.isLp);

		foreach (var x in newItems) {
			if (x.BaseObject is FileSystemInfo info) this.items.Add(info);
			else throw new Exception($"Unsupported item type: {x.BaseObject.GetType()}");
		}
	}

	protected override void EndProcessing() {
		if (this.items.Count == 0) return;
		else if (this.items.Count == 1 && this.inputCount == 1 && this.items[0] is DirectoryInfo dir) {
			this.items.Clear();

			var opts = new EnumerationOptions() {
				AttributesToSkip = FileAttributes.Device | FileAttributes.System,
				IgnoreInaccessible = true,
				ReturnSpecialDirectories = false,
			};

			foreach (var item in dir.EnumerateFileSystemInfos("*", opts)) {
				this.items.Add(item);
			}
		}

		// TODO: Parallelize this.
		var sizes = this.items.Select(item => new SizeInfo {
			Size = Fs.MeasureSize(item, this.CheckCancel),
			Item = item,
		})
		.OrderBy(x => x.Size);

		foreach (var x in sizes) WriteObject(x);
	}
}
