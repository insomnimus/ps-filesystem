namespace Filesystem;

using System.Text;
using System.Globalization;

public struct ByteSize: IComparable<ByteSize> {
	private static (string, long, long)[] Units = [
		("EiB", 1L << 60, (long)1e18),
		("PiB", 1L << 50, (long)1e15),
		("TiB", 1L << 40, (long)1e12),
		("GiB", 1L << 30, (long)1e9),
		("MiB", 1L << 20, (long)1e6),
		("KiB", 1L << 10, 1000),
	];

	private long bytes = 0;

	public ByteSize() { }
	public ByteSize(long bytes) => this.bytes = bytes;
	public ByteSize(int bytes) => this.bytes = bytes;
	public ByteSize(ulong bytes) => this.bytes = (long)bytes;
	public ByteSize(uint bytes) => this.bytes = (long)bytes;

	public int CompareTo(ByteSize other) => this.bytes.CompareTo(other.bytes);

	public override string ToString() {
		var b = System.Math.Abs(this.bytes);
		foreach (var (unit, multiplier, threshold) in Units) {
			if (b >= threshold) {
				var d = new decimal(this.bytes) / multiplier;
				return d.ToString("F2", CultureInfo.InvariantCulture)
				.TrimEnd('0')
				.TrimEnd('.') + unit;
			}
		}

		return $"{this.bytes}B";
	}

	// Operator overloads
	public static implicit operator ByteSize(long n) => new ByteSize(n);
	public static implicit operator ByteSize(int n) => new ByteSize(n);
	public static implicit operator ByteSize(uint n) => new ByteSize(n);

	public static explicit operator ByteSize(ulong n) => new ByteSize((long)n);
	public static explicit operator ByteSize(double n) => new ByteSize((long)n);
	public static explicit operator ByteSize(decimal n) => new ByteSize(decimal.ToInt64(n));

	public static explicit operator long(ByteSize b) => b.bytes;
	public static explicit operator int(ByteSize b) => (int)b.bytes;
	public static explicit operator ulong(ByteSize b) => (ulong)b.bytes;
	public static explicit operator uint(ByteSize b) => (uint)b.bytes;
	public static explicit operator double(ByteSize b) => (double)b.bytes;
	public static explicit operator decimal(ByteSize b) => (decimal)b.bytes;

	public static ByteSize operator +(ByteSize a, ByteSize b) => new ByteSize(a.bytes + b.bytes);
	public static ByteSize operator +(ByteSize a, ulong b) => new ByteSize(a.bytes + (long)b);
	public static ByteSize operator +(ByteSize a, uint b) => new ByteSize(a.bytes + b);
	public static ByteSize operator +(ByteSize a, long b) => new ByteSize(a.bytes + b);
	public static ByteSize operator +(ByteSize a, int b) => new ByteSize(a.bytes + b);

	public static ByteSize operator -(ByteSize a, ByteSize b) => new ByteSize(a.bytes - b.bytes);
	public static ByteSize operator -(ByteSize a, long b) => new ByteSize(a.bytes - b);
	public static ByteSize operator -(ByteSize a, int b) => new ByteSize(a.bytes - b);
	public static ByteSize operator -(ByteSize a, ulong b) => new ByteSize(a.bytes - (long)b);
	public static ByteSize operator -(ByteSize a, uint b) => new ByteSize(a.bytes - (long)b);

	public static ByteSize operator *(ByteSize a, ByteSize b) => new ByteSize(a.bytes * b.bytes);
	public static ByteSize operator *(ByteSize a, long b) => new ByteSize(a.bytes * b);
	public static ByteSize operator *(ByteSize a, int b) => new ByteSize(a.bytes * b);
	public static ByteSize operator *(ByteSize a, ulong b) => new ByteSize(a.bytes * (long)b);
	public static ByteSize operator *(ByteSize a, uint b) => new ByteSize(a.bytes * b);
	public static ByteSize operator *(ByteSize a, double b) => (ByteSize)((double)a.bytes * b);
	public static ByteSize operator *(ByteSize a, decimal b) => (ByteSize)((decimal)a.bytes * b);

	public static double operator /(ByteSize a, ByteSize b) => (double)a.bytes / (double)b.bytes;
	public static ByteSize operator /(ByteSize a, long b) => new ByteSize(a.bytes / b);
	public static ByteSize operator /(ByteSize a, int b) => new ByteSize(a.bytes / b);
	public static ByteSize operator /(ByteSize a, ulong b) => new ByteSize(a.bytes / (long)b);
	public static ByteSize operator /(ByteSize a, uint b) => new ByteSize(a.bytes / b);
	public static ByteSize operator /(ByteSize a, double b) => (ByteSize)((double)a.bytes / b);
	public static ByteSize operator /(ByteSize a, decimal b) => (ByteSize)((decimal)a.bytes / b);

	// Methods
	public long Bytes() => this.bytes;

	public static bool TryParse(string s, out ByteSize b, out string error) {
		b = new ByteSize();
		s = s?.Trim();

		if (string.IsNullOrEmpty(s)) {
			error = "The input is empty";
			return false;
		}

		var i = 0;
		for (; i < s.Length; i++) {
			var c = s[i];
			if (c != '.' && !char.IsDigit(c)) break;
		}

		var numberPart = s.Substring(0, i);
		var unitPart = s.Substring(i).TrimStart();

		if (string.IsNullOrEmpty(numberPart)) {
			error = "Missing numeric part";
			return false;
		}

		decimal n;
		try {
			n = decimal.Parse(numberPart);
		} catch (Exception e) {
			error = e.ToString();
			return false;
		}

		decimal multiplier = unitPart.ToLower(CultureInfo.InvariantCulture) switch {
			"b" or "" or "byte" or "bytes" => 1,
			"k" or "kb" or "kilobyte" or "kilobytes" => 1000,
			"ki" or "kib" or "kibibyte" or "kibibytes" => 1024m,
			"m" or "mb" or "megabyte" or "megabytes" => 1e6m,
			"mi" or "mib" or "mebibyte" or "mebibytes" => new decimal(1L << 20),
			"g" or "gb" or "gigabyte" or "gigabytes" => 1e9m,
			"gi" or "gib" or "gibibyte" or "gibibytes" => new decimal(1L << 30),
			"t" or "tb" or "terabyte" or "terabytes" => 1e12m,
			"ti" or "tib" or "tebibyte" or "tebibytes" => new decimal(1L << 40),
			"p" or "pb" or "petabyte" or "petabytes" => 1e15m,
			"pi" or "pib" or "pebibyte" or "pebibytes" => new decimal(1L << 50),
			"e" or "eb" or "exabyte" or "exabytes" => 1e18m,
			"ei" or "eib" or "exbibyte" or "exbibytes" => new decimal(1L << 60),
			"z" or "zb" or "zettabyte" or "zettabytes" => 1e21m,
			"zi" or "zib" or "zebibyte" or "zebibytes" => new decimal(1L << 60) * 1024m,
			_ => decimal.Zero,
		};

		if (multiplier == decimal.Zero) {
			error = $"Unknown unit '{unitPart}'";
			return false;
		}

		try {
			checked {
				n *= multiplier;
				b = new ByteSize(decimal.ToInt64(n));
			}
			error = "";
			return true;
		} catch (Exception e) {
			error = e.ToString();
			return false;
		}
	}

	public static bool TryParse(string s, out ByteSize b) {
		string _error;
		return ByteSize.TryParse(s, out b, out _error);
	}

	public static ByteSize Parse(string s) {
		string error;
		ByteSize b;
		ByteSize.TryParse(s, out b, out error);
		if (error != "") {
			throw new FormatException(error);
		}
		return b;
	}
}

public record struct DirEntry(FileSystemInfo Item, uint Depth, bool IsDir);

internal static class Fs {
	internal static bool IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
	internal static char[] PathSeparators => IsWindows ? ['\\', '/'] : ['/'];

	private static Random RNG = new Random();

	internal static ByteSize MeasureSize(FileSystemInfo info, Action checkCancel) {
		if (info.Attributes.HasFlag(FileAttributes.Device) || info.Attributes.HasFlag(FileAttributes.ReparsePoint)) {
			return new ByteSize(0);
		}

		var q = new Queue<DirectoryInfo>();
		if (info is FileInfo _f) return new ByteSize(_f.Length);
		else if (info is DirectoryInfo _d) q.Enqueue(_d);
		else return new ByteSize();

		var opts = new EnumerationOptions() {
			AttributesToSkip = FileAttributes.Device | FileAttributes.System | FileAttributes.ReparsePoint,
			IgnoreInaccessible = true,
			// We don't use RecurseSubdirectories because there's no way to tell it to not traverse symlink dirs.
			// RecurseSubdirectories = true,
			ReturnSpecialDirectories = false,
		};

		long n = 0;
		DirectoryInfo d;

		for (var i = 0; q.TryDequeue(out d); i++) {
			checkCancel();
			try {
				foreach (var x in d.EnumerateFileSystemInfos("*", opts)) {
					if (x is FileInfo f) n += f.Length;
					else if (x is DirectoryInfo dir) q.Enqueue(dir);
				}
			} catch (Exception e) {
				if (i == 0) throw e;
				// Ignore it otherwise
			}
		}

		return new ByteSize(n);
	}

	public static bool Exists(string p) {
		try {
			File.GetAttributes(p);
			return true;
		} catch (Exception) {
			return false;
		}
	}

	public static string RandomFileName(ushort randomLength = 16, string prefix = "", string suffix = "") {
		const string CHARS = "abcdefghijklmnopqrstuvwxyz0123456789-_+^%&=";
		var buf = new StringBuilder((int)randomLength + prefix.Length + suffix.Length);
		buf.Append(prefix);

		for (var i = 0; i < randomLength; i++) {
			buf.Append(CHARS[RNG.Next(CHARS.Length)]);
		}

		buf.Append(suffix);
		return buf.ToString();
	}

	public static string TrimEndingDirectorySeparator(string p) {
		if (p == "/" || (IsWindows && p == "\\")) {
			return p;
		}
		return p.TrimEnd(PathSeparators);
	}

	public static IEnumerable<DirEntry> Walk(DirectoryInfo root, Func<DirectoryInfo, uint, bool> dirFilter) {
		var dirs = new Queue<(DirectoryInfo dir, uint depth)>();
		dirs.Enqueue((root, 0));

		var opts = new EnumerationOptions {
			AttributesToSkip = FileAttributes.Device | FileAttributes.System,
			IgnoreInaccessible = true,
			ReturnSpecialDirectories = false,
		};
		(DirectoryInfo dir, uint depth) cur;

		while (dirs.TryDequeue(out cur)) {
			foreach (var x in cur.dir.EnumerateFileSystemInfos("*", opts)) {
				if (x is DirectoryInfo d) {
					if (dirFilter(d, cur.depth + 1)) {
						dirs.Enqueue((d, cur.depth + 1));
						yield return new DirEntry(x, cur.depth + 1, true);
					}
				} else {
					yield return new DirEntry(x, cur.depth + 1, false);
				}
			}
		}
	}

	internal static string HomePath() {
		return IsWindows
? $"{Environment.GetEnvironmentVariable("HOMEDRIVE")}\\{Environment.GetEnvironmentVariable("HOMEPATH")}"
: Environment.GetEnvironmentVariable("HOME");
	}
}
