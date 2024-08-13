public static class FilePath {
	public static char[] Separators = Fs.IsWindows ? ['\\', '/'] : ['/'];
	public static char Separator => Path.DirectorySeparatorChar;
	public static bool IsSeparator(char c) {
		return c == '/' || (Fs.IsWindows && c == '\\');
	}

	public static string Normalize(string p) {
		if (string.IsNullOrEmpty(p)) return "";

		var (prefix, rest) = findPrefix(p);
		if (rest == "") {
			return p;
		}

		var components = rest.Split(['\\', '/']);
		var buf = new List<string>(components.Length + 1);

		foreach (var s in components) {

			switch (s) {
				case "": break;
				case ".": break;
				case ".." when buf.Count > 0:
					if (buf[buf.Count - 1] == "..") {
						buf.Add(s);
					} else {
						buf.RemoveAt(buf.Count - 1);
					}
					break;
				default:
					buf.Add(s);
					break;
			}
		}

		if (prefix != "") {
			if (Fs.IsWindows) {
				prefix = prefix.Replace('/', '\\');
			}
			buf.Insert(0, prefix);
		}

		if (buf.Count == 0) return ".";
		else return Path.Combine(buf.ToArray());
	}

	private static (string, string) findPrefix(string p) {
		// ON non Windows platforms the only prefix is a leading / or a powershell-only drive.
		if (!Fs.IsWindows) {
			if (p.StartsWith('/')) {
				return ("/", p.TrimStart('/'));
			}

			// It might be a path like variable:/foo.
			// Also here the \ separator is allowed.
			var i = p.Find(['/', '\\']);
			if (i > 1 && isDrive(p[0..(i + 1)])) return (p[0..(i + 1)], p[(i + 1)..].TrimStart('/'));
			else if (i < 0 && isDrive(p)) return (p, "");
			else return ("", p);
		}

		// On Windows a prefix is either a drive letter, a UNC paths first section or simply / or \.
		if (p.StartsWith("//") || p.StartsWith(@"\\")) {
			var i = p.Find(['\\', '/'], 2);
			if (i < 0) {
				if (p.Length > 2) {
					return p.SplitOnce(c => !IsSeparator(c));
				} else {
					return (p, "");
				}
			} else if (i == 2) {
				// It starts with triple slashes; therefore not a valid UNC path.
				return (p.SplitOnce(c => !IsSeparator(c)));
			} else {
				// It's a valid UNC path.
				return p.SplitAt(i + 1);
			}
		} else {
			// Does it start with a drive letter?
			// For our use case, a Powershell drive is also valid; e.g. Foo:\
			// And if it doesn't, does it start with a separator?
			var i = p.Find(['\\', '/']);
			if (i == 0) {
				return p.SplitOnce(c => !IsSeparator(c));
			} else if (i < 0) {
				// Is it a drive directly?
				if (isDrive(p)) return (p, "");
				else return ("", p);
			} else {
				// Does it start with a drive?
				var s = p.Substring(0, i + 1);
				if (isDrive(s)) {
					var rest = p.Substring(i + 1);
					return (s, rest.TrimStart(['\\', '/']));
				} else {
					// It's a relative path.
					return ("", p);
				}
			}
		}
	}

	private static bool isDrive(string s) {
		// Normally a drive letter would be letters A-Z, followed by a colon and optionally a path separator.
		// But since our use case includes Powershell custom drives, we need to allow more.
		const string DISALLOWED = @";~/\.:";

		if (s.Length < 2) {
			return false;
		}

		var to = s.Length;
		if (s[s.Length - 1] == '\\' || s[s.Length - 1] == '/') {
			to--;
		}
		if (s[to - 1] != ':') {
			return false;
		}

		return to > 1 && s.Take(to - 1).All(c => !DISALLOWED.Contains(c));
	}

	// For correct behaviour, both of the arguments must be absolute paths.
	internal static string GetRelativePath(string path, string relativeTo) {
		path = Normalize(path);
		relativeTo = Normalize(relativeTo);

		var (pPrefix, pRest) = findPrefix(path);
		var (rPrefix, rRest) = findPrefix(relativeTo);

		if (!pPrefix.OsEq(rPrefix)) {
			return path;
		}

		var p = pRest.Split(Separators, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);
		var r = rRest.Split(Separators, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);

		var min = Math.Min(p.Length, r.Length);
		var back = 0;
		IEnumerable<string> forward = null;

		for (var i = 0; i < min; i++) {
			if (!p[i].OsEq(r[i])) {
				back = r.Length - i;
				forward = p.Skip(i);
				break;
			}
		}

		// If the loop above didn't break early
		if (forward is null) {
			back = r.Length - min;
			forward = p.Skip(min);
		}

		var buf = new List<string>(Math.Max(p.Length, r.Length));

		for (var i = 0; i < back; i++) {
			buf.Add("..");
		}
		foreach (var s in forward) {
			buf.Add(s);
		}

		if (buf.Count == 0) return ".";
		else return Path.Combine(buf.ToArray());
	}

	public static bool IsAbsolute(string p) {
		var (prefix, _) = findPrefix(p);
		return prefix != "";
	}

	public static string Combine(string a, string b) {
		if (string.IsNullOrEmpty(b)) return a;
		else if (string.IsNullOrEmpty(a)) return b;

		// On windows if `a` starts with a drive letter
		// and `b` starts with \or / and is not a UNC path;
		// preserve the drive letter.
		if (!Fs.IsWindows || !IsSeparator(b[0])) {
			return Path.Combine(a, b);
		}

		// Does `a` start with a drive letter?
		var aFirstSep = a.Find(Separators);
		if (aFirstSep <= 1) {
			goto regular;
		}
		var prefix = a.Substring(0, aFirstSep + 1);
		if (!isDrive(prefix)) {
			goto regular;
		}

		int i = 0;
		// Is `b` a UNC path?
		if (b.StartsWith(@"\\") || b.StartsWith("//")) {
			i = b.Find(c => !IsSeparator(c), 2);
			if (i < 0) {
				goto regular;
			}
			var sepPos = b.Find(Separators, i);
			if (sepPos > 0) {
				goto regular;
			}

			// It's a path like \\foo (without a trailing separator), which is not a valid UNC path.
			return Path.Join(prefix, b.Substring(i));
		}

		// `b` is not a UNC path but it might start with multiple mismatched slashes (e.g. /\)
		i = b.Find(c => !IsSeparator(c));
		if (i < 0) {
			// `b` consists of separators.
			return "\\";
		}
		return Path.Join(prefix, b.Substring(i));

	regular: return Path.Combine(a, b);
	}

	public static string Combine(string a, string b, bool normalize) {
		var s = Combine(a, b);
		if (normalize) return Normalize(s);
		else return s;
	}

	public static string Parent(string p) {
		var (prefix, rest) = findPrefix(p);
		if (rest == "") {
			return "";
		}

		// Ignore trailing path separators.
		var to = rest.RFind(c => !IsSeparator(c));
		if (to < 0) return "";

		// Find the last path separator that's not trailing.
		to = rest.RFind(Separators, to);
		if (to < 0) return prefix;

		// Now trim any remaining separators.
		to = rest.RFind(c => !IsSeparator(c), to);
		if (to < 0) return prefix;
		else return Path.Join(prefix, rest[0..(to + 1)]);
	}

	internal static (string prefix, string[] components) Components(string p) {
		var (prefix, rest) = findPrefix(p);
		return (prefix, rest.Split(Separators, int.MaxValue, StringSplitOptions.RemoveEmptyEntries));
	}

	// Tests if `path` starts with `_with`.
	internal static bool StartsWith(string path, string _with, bool normalize = true) {
		if (normalize) {
			path = Normalize(path);
			_with = Normalize(_with);
		}

		var (pathPrefix, pathComps) = Components(path);
		var (withPrefix, withComps) = Components(_with);
		if (withComps.Length > pathComps.Length || !withPrefix.OsEq(pathPrefix)) {
			return false;
		}

		for (var i = 0; i < withComps.Length; i++) {
			if (!withComps[i].OsEq(pathComps[i])) {
				return false;
			}
		}

		return true;
	}

	public static bool StartsWith(string[] path, string[] _with) {
		if (path.Length < _with.Length) {
			return false;
		}

		for (var i = 0; i < _with.Length; i++) {
			if (!path[i].OsEq(_with[i])) {
				return false;
			}
		}

		return true;
	}

	public static Span<string> StripPrefix(string[] path, string[] what) {
		if (StartsWith(path, what)) {
			return new(path, what.Length, path.Length - what.Length);
		} else {
			return new(path);
		}
	}
}
