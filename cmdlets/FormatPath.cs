using System.Text;

[Cmdlet(VerbsCommon.Format, "Path", DefaultParameterSetName = "pretty")]
[OutputType(typeof(string))]
public class FormatPath: Cmd {
	[Parameter(
		Mandatory = true,
		Position = 0,
		ValueFromPipeline = true,
		HelpMessage = "Path to normalize"
	)]
	public string Path { get; set; }

	[Parameter(ParameterSetName = "normalize", HelpMessage = "Only perform normalization")]
	public SwitchParameter Normalize { get; set; }

	[Parameter(ParameterSetName = "pretty", HelpMessage = "Try to optimize the output for human-readability")]
	public SwitchParameter Pretty { get; set; }
	[Parameter(ParameterSetName = "pretty", HelpMessage = "Get a relative path (or if -Pretty is provided, base decisions on a different path than $PWD)")]
	public string RelativeTo { get; set; }

	private string home = "";

	protected override void BeginProcessing() {
		if (this.Pretty) {
			this.home = Fs.HomePath();
		}
	}

	protected override void ProcessRecord() {
		if (this.Normalize) {
			WriteObject(FilePath.Normalize(this.Path));
		} else if (this.Pretty) {
			WriteObject(this.formatPretty());
		} else {
			WriteObject(FilePath.GetRelativePath(
				FilePath.Combine(this.PWD, this.Path, true),
				FilePath.Combine(this.PWD, this.RelativeTo)
			));
		}
	}

	private string formatPretty() {
		var path = FilePath.Combine(this.PWD, this.Path, true);
		var p = FilePath.Components(path);
		var pwd = FilePath.Components(FilePath.Combine(this.PWD, this.RelativeTo, true));

		if (!p.prefix.OsEq(pwd.prefix)) {
			var h = FilePath.Components(this.home);
			var fromHome = FilePath.StripPrefix(p.components, h.components);
			// Is it under $HOME?
			if (fromHome.Length != p.components.Length && h.prefix.OsEq(p.prefix)) {
				return joinHome(fromHome);
			} else {
				// It's in a different prefix.
				return FilePath.Normalize(this.Path);
			}
		}

		// pwd and p are in the same root; do not try to format based off $HOME.
		var steps = calcSteps(pwd.components, p.components);
		// Some heuristic for whether to use relative or absolute paths.
		if (steps.relativeLength + 1 > p.components.Length) {
			return path;
		} else {
			return relPath(p.components, steps.back, steps.skip);
		}
	}

	private (int back, int skip, int relativeLength) calcSteps(string[] relativeTo, string[] p) {
		var min = Math.Min(p.Length, relativeTo.Length);
		var i = 0;
		for (; i < min; i++) {
			if (!relativeTo[i].OsEq(p[i])) {
				break;
			}
		}

		var back = relativeTo.Length - i;
		var skip = i;
		var relativeLength = back + p.Length - skip;

		return (back, skip, relativeLength);
	}

	private static string joinHome(Span<string> components) {
		if (components.IsEmpty) return Fs.IsWindows ? "~\\" : "~/";

		var len = 1;
		foreach (var s in components) len += 1 + s.Length;

		var buf = new StringBuilder(len);
		buf.Append("~");
		foreach (var s in components) {
			buf.Append(FilePath.Separator);
			buf.Append(s);
		}

		return buf.ToString();
	}

	private static string relPath(string[] components, int back, int skip) {
		if (back == 0 && skip >= components.Length) return ".";

		var buf = new StringBuilder(128);
		for (var i = 0; i < back; i++) {
			if (buf.Length > 0) buf.Append(FilePath.Separator);
			buf.Append("..");
		}

		foreach (var s in components.Skip(skip)) {
			if (buf.Length > 0) buf.Append(FilePath.Separator);
			buf.Append(s);
		}

		return buf.ToString();
	}
}
