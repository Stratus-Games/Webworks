namespace Webworks.Internal
{
	internal static class MiniJson
	{
		public static string ExtractString(string json, string key)
		{
			if (string.IsNullOrEmpty(json)) return null;
			int idx = json.IndexOf($"\"{key}\"");
			if (idx < 0) return null;

			int colon = json.IndexOf(':', idx);
			int firstQuote = json.IndexOf('"', colon + 1);
			if (firstQuote < 0) return null;

			int secondQuote = firstQuote + 1;
			while (secondQuote < json.Length && json[secondQuote] != '"')
			{
				if (json[secondQuote] == '\\') secondQuote++;
				secondQuote++;
			}
			if (secondQuote >= json.Length) return null;

			return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1)
				.Replace("\\\"", "\"")
				.Replace("\\\\", "\\");
		}

		public static int ExtractInt(string json, string key, int fallback = 0)
		{
			var raw = ExtractNumberRaw(json, key);
			int value;
			return raw != null && int.TryParse(raw, out value) ? value : fallback;
		}

		public static uint ExtractUInt(string json, string key, uint fallback = 0)
		{
			var raw = ExtractNumberRaw(json, key);
			uint value;
			return raw != null && uint.TryParse(raw, out value) ? value : fallback;
		}

		public static bool ExtractBool(string json, string key, bool fallback = false)
		{
			if (string.IsNullOrEmpty(json)) return fallback;
			int idx = json.IndexOf($"\"{key}\"");
			if (idx < 0) return fallback;

			int colon = json.IndexOf(':', idx);
			int end = colon + 1;
			while (end < json.Length && char.IsWhiteSpace(json[end])) end++;

			if (json.Substring(end).StartsWith("true")) return true;
			if (json.Substring(end).StartsWith("false")) return false;
			return fallback;
		}

		public static int CountArrayEntries(string json, string field)
		{
			if (string.IsNullOrEmpty(json)) return 0;
			int idx = json.IndexOf($"\"{field}\"");
			if (idx < 0) return 0;

			int start = json.IndexOf('[', idx);
			int end = json.IndexOf(']', start);
			if (start < 0 || end < 0) return 0;

			var inner = json.Substring(start + 1, end - start - 1).Trim();
			return inner.Length == 0 ? 0 : inner.Split(',').Length;
		}

		static string ExtractNumberRaw(string json, string key)
		{
			if (string.IsNullOrEmpty(json)) return null;
			int idx = json.IndexOf($"\"{key}\"");
			if (idx < 0) return null;

			int colon = json.IndexOf(':', idx);
			int start = colon + 1;
			while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

			int end = start;
			while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-' || json[end] == '.')) end++;

			return end > start ? json.Substring(start, end - start) : null;
		}
	}
}
