namespace Filesystem;

using System.Collections.ObjectModel;

public enum TestItemType {
	Any,
	File,
	Directory,
	SparseFile,
}

[Cmdlet(VerbsDiagnostic.Test, "Item")]
[OutputType(typeof(bool))]
public class TestItem: PSCmdlet {
	[Parameter(
		Mandatory = true,
		Position = 0,
		ValueFromPipeline = true,
		HelpMessage = "Path to the item to test"
	)]
	public object[] Path { get; set; }

	[Parameter(HelpMessage = "Item type")]
	public TestItemType Type { get; set; } = TestItemType.Any;

	[Parameter(HelpMessage = "Test if item is empty")]
	public SwitchParameter Empty { get; set; }
	[Parameter(HelpMessage = "Test if item is a symbolic link or an NTFS junction")]
	public SwitchParameter Link { get; set; }

	protected override void ProcessRecord() {
		var items = new List<FileSystemInfo>(this.Path.Length);
		foreach (var p in this.Path.Where(x => x is not null)) {
			Collection<PSObject> res = null;
			try {
				res = this.InvokeProvider.Item.Get([p.ToString()], true, true);
			} catch (Exception) {
				WriteObject(false);
			}

			foreach (var x in res) {
				if (x.BaseObject is FileSystemInfo i) items.Add(i);
				else throw new Exception($"Testing for non FileSystem items is not implemented: {x}");
			}
		}

		foreach (var item in items) {
			WriteObject(this.isMatch(item));
		}
	}

	private bool isMatch(FileSystemInfo x) {
		if ((this.Link && !x.Attributes.HasFlag(FileAttributes.ReparsePoint)) || (this.Type == TestItemType.SparseFile && !x.Attributes.HasFlag(FileAttributes.SparseFile))) {
			return false;
		}

		if (x is FileInfo f) {
			return this.Type switch {
				TestItemType.Any or TestItemType.File or TestItemType.SparseFile => !this.Empty || f.Length == 0,
				_ => false,
			};
		} else if (this.Type != TestItemType.Directory && this.Type != TestItemType.Any) {
			return false;
		} else {
			if (this.Empty) {
				var iter = Directory.EnumerateFileSystemEntries(x.FullName).GetEnumerator();
				return !iter.MoveNext();
			} else {
				return true;
			}
		}
	}
}
