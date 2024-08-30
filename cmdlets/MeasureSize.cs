namespace Filesystem;

[Cmdlet(
VerbsDiagnostic.Measure, "Size",
	DefaultParameterSetName = "wildcard"
)]
[OutputType(typeof(ByteSize))]
public class MeasureSize: Cmd {
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

	private ByteSize size = new();
	private bool isLp = false;

	protected override void BeginProcessing() {
		this.isLp = this.ParameterSetName == "lp";
	}

	protected override void ProcessRecord() {
		var strings = (this.isLp ? this.LiteralPath : this.Path)
		.Where(x => x is not null)
		.Select(x => x.ToString()).ToArray();
		var newItems = this.InvokeProvider.Item.Get(strings, true, this.isLp);

		foreach (var x in newItems) {
			if (x.BaseObject is FileSystemInfo info) this.size += Fs.MeasureSize(info, this.CheckCancel);
			else throw new Exception($"Unsupported item type: {x.BaseObject.GetType()}");
		}
	}

	protected override void EndProcessing() {
		WriteObject(this.size);
	}
}
