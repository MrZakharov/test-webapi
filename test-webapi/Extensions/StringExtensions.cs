using System.Text.RegularExpressions;

namespace test_webapi.Extensions {
	public static class StringExtensions {
		readonly static string badChars = "*/\\\\?|:\\\"><";

		public static string ClearUnsafePath(this string value) {
			Regex reg = new("[" + badChars + "]");
			return reg.Replace(value, string.Empty).Replace("..", string.Empty).Replace("..", string.Empty).Trim();
		}
	}
}
