using System.Collections.ObjectModel;

internal class PathBuf: ICloneable {
	private static char[] Separators => Fs.IsWindows ? ['\\', '/'] : ['/'];
	public static char Separator => Separators[0];

	// A hack, used as a root component.
	private const string ROOT = "";

	private List<string> components;
	private bool normalized = false;

	public PathBuf() {
		this.components = new();
	}

	public PathBuf(string path) {
		this.components = new();
		this.Append(path);
	}

	internal bool IsEmpty => this.components.Count == 0;

	public PathBuf Clone() {
		var p = new PathBuf();
		p.components.Capacity = this.components.Capacity;
		p.components.AddRange(this.components);
		return p;
	}

	object ICloneable.Clone() => this.Clone();

	public string Pop() {
		if (this.components.Count == 0) return "";
		else if (this.components.Count == 1 && this.components[0] == ROOT) {
			this.components.Clear();
			return Separator.ToString();
		}

		var s = this.components[this.components.Count - 1];
		this.components.RemoveAt(this.components.Count - 1);
		return s;
	}

	public override string ToString() {
		this.Normalize();

		if (Fs.IsWindows && this.components.Count == 1 && this.components[0].EndsWith(':')) return this.components[0] + "\\";
		else if (this.components.Count == 1 && this.components[0] == ROOT) return Separator.ToString();
		else return string.Join(Separator, this.components);
	}

	public void Normalize() {
		if (this.normalized) return;

		var comps = new List<string>(this.components.Count);
		foreach (var s in this.components) {
			switch (s) {
				case ".": break;
				case ".." when comps.Count > 0:
					var last = comps[comps.Count - 1];
					// Do not remove if last is "..", `ROOT`, or on Windows additionally a drive.
					if (last != ".." && last != ROOT && (!Fs.IsWindows || comps.Count > 1 || !last.EndsWith(':'))) {
						comps.RemoveAt(comps.Count - 1);
					} else {
						comps.Add(s);
					}
					break;
				default:
					comps.Add(s);
					break;
			}
		}

		this.components = comps;
		this.normalized = true;
	}

	public void Append(string p) {
		this.normalized = false;

		// If what we're appending starts with a path separator, clear buffer.
		if (Separators.Any(sep => p.StartsWith(sep))) {
			this.components.Clear();
			this.components.Add(ROOT);
		}

		var comps = p.Split(Separators);

		int i = -1;
		foreach (var s in comps) {
			i++;
			if (s == "") continue;

			if (Fs.IsWindows) {
				var colonPos = s.LastIndexOf(':');
				// If what we're appending starts with a drive letter, clear buffer.
				if (i == 0 && colonPos == s.Length - 1) {
					this.components.Clear();
					this.components.Add(s);
					continue;
				} else if (colonPos >= 0) {
					throw new ArgumentException($"Windows paths cannot contain ':' except for drive letters: {p}");
				}
			}

			if (s != ".") {
				this.components.Add(s);
			}
		}
	}

	public static PathBuf Join(string p1, params string[] rest) {
		var path = new PathBuf(p1);
		foreach (var p in rest) path.Append(p);
		return path;
	}

	public ReadOnlyCollection<string> Components() {
		this.Normalize();
		return this.components.AsReadOnly();
	}

	public string Parent() {
		var p = this.Clone();
		if (p.Pop() == "") return "";
		else if (p.IsEmpty) return "";
		else return p.ToString();
	}
}
