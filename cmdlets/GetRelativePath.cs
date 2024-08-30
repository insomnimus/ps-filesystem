[Cmdlet(VerbsCommon.Get, "RelativePath")]
[OutputType(typeof(string))]
public class GetRelativePath: Cmd {
	[Parameter(
		Mandatory = true,
		Position = 0,
		ValueFromPipeline = true,
		HelpMessage = "Path to make relative"
	)]
	public string Path { get; set; }

	[Parameter(HelpMessage = "Make path relative to another directory")]
	public string Base { get; set; } = ".";

	protected override void ProcessRecord() {
		var relativeTo = FilePath.Combine(this.PWD, this.Base);
		var p = FilePath.Combine(this.PWD, this.Path);

		WriteObject(FilePath.GetRelativePath(p, relativeTo));
	}
}
