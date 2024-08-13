public static class StringExt {
	private static bool IsWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

	public static bool OsEq(this string a, string b) {
		if (IsWindows) return a.EqIgnoreCase(b);
		else return a == b;
	}

	public static bool EqIgnoreCase(this string a, string b) {
		return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
	}

	public static int Find(this string s, Predicate<char> pred, int start = 0) {
		if (string.IsNullOrEmpty(s)) return -1;

		for (var i = 0; i < s.Length; i++) {
			if (pred(s[i])) return i;
		}

		return -1;
	}

	public static int Find(this string s, char[] chars, int start = 0) {
		if (start >= s.Length) return -1;

		for (var i = start; i < s.Length; i++) {
			if (chars.Contains(s[i])) {
				return i;
			}
		}

		return -1;
	}

	public static int RFind(this string s, char[] chars, int start = -1) {
		return s.RFind(c => chars.Contains(c), start);
	}

	public static int RFind(this string s, Predicate<char> pred, int start = -1) {
		if (start < 0) start = s.Length - 1;

		for (var i = start; i >= 0; i--) {
			if (pred(s[i])) {
				return i;
			}
		}
		return -1;
	}

	public static (string, string) SplitAt(this string s, int index) {
		return (s.Substring(0, index), s.Substring(index));
	}

	public static (string, string) SplitOnce(this string s, Predicate<char> pred) {
		for (var i = 0; i < s.Length; i++) {
			if (pred(s[i])) {
				return s.SplitAt(i);
			}
		}

		return (s, "");
	}

	public static (string, string) SplitOnce(this string s, char[] chars) {
		return s.SplitOnce(c => chars.Contains(c));
	}

	public static (string, string) SplitOnce(this string s, char c) {
		return s.SplitOnce(_c => _c == c);
	}
}
