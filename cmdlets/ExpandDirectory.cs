using IoPath = System.IO.Path;

[Cmdlet(VerbsData.Expand, "Directory")]
public class ExpandDirectory: PSCmdlet {
	[Parameter(
		Mandatory = true,
		Position = 0,
		HelpMessage = "Directory to unnest"
	)]
	public string Path { get; set; }

	protected override void ProcessRecord() {
		var item = this.InvokeProvider.Item.Get([this.Path], true, true)[0];
		DirectoryInfo dir;
		if (item.BaseObject is DirectoryInfo _d) {
			dir = _d;
		} else {
			throw new Exception($"Path does not point to a directory: {this.Path}");
		}

		var parent = FilePath.Parent(dir.FullName);
		if (parent == "") {
			throw new Exception("You cannot unnest the root directory");
		}

		var children = dir.GetFileSystemInfos();
		var renameDir = false;

		foreach (var x in children) {
			var target = IoPath.Combine(parent, x.Name);
			// Do not use OsEq because Linux can have case insensitive filesystems too.
			if (!renameDir && string.Equals(x.Name, dir.Name, StringComparison.CurrentCultureIgnoreCase)) {
				renameDir = true;
			} else if (Fs.Exists(target)) {
				throw new Exception($"Directory contains children that exists in the parent: {x.Name}");
			}
		}

		if (renameDir) {
			var temp = IoPath.Combine(parent, Fs.RandomFileName(16, ".", ".tmp"));
			WriteVerbose($"temporarily renaming directory {dir} to {temp}");
			dir.MoveTo(temp);

			foreach (var x in children) {
				var src = IoPath.Combine(temp, x.Name);
				var dest = IoPath.Combine(parent, x.Name);

				if (x.Attributes.HasFlag(FileAttributes.Directory)) {
					Directory.Move(src, dest);
				} else {
					File.Move(src, dest);
				}
			}

			WriteVerbose($"Deleting directory {temp}");
			Directory.Delete(temp);
			return;
		}

		foreach (var x in children) {
			var dest = IoPath.Combine(parent, x.Name);

			if (x is FileInfo f) {
				f.MoveTo(dest);
			} else if (x is DirectoryInfo d) {
				d.MoveTo(dest);
			}
		}

		WriteVerbose($"Deleting directory {dir}");
		dir.Delete();
	}
}
