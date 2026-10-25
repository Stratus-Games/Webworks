using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Webworks.SNetInternal
{
	internal static class WebworksJson
	{
		public static object Parse(string s)
		{
			if (string.IsNullOrEmpty(s)) return null;
			try { int i = 0; return ReadValue(s, ref i); }
			catch (Exception) { return null; }
		}

		public static Dictionary<string, object> Obj(string s) { return Parse(s) as Dictionary<string, object>; }

		public static string Quote(string s)
		{
			if (s == null) return "null";
			var sb = new StringBuilder("\"");
			foreach (var c in s)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
						else sb.Append(c);
						break;
				}
			}
			return sb.Append('"').ToString();
		}

		public static string Str(Dictionary<string, object> d, string key)
		{
			object v;
			if (d == null || !d.TryGetValue(key, out v) || v == null) return null;
			if (v is string) return (string)v;
			if (v is bool) return (bool)v ? "true" : "false";
			if (v is double) return ((double)v).ToString(CultureInfo.InvariantCulture);
			return null;
		}

		public static double Num(Dictionary<string, object> d, string key, double fallback = 0)
		{
			object v;
			if (d == null || !d.TryGetValue(key, out v) || v == null) return fallback;
			if (v is double) return (double)v;
			double parsed;
			if (v is string && double.TryParse((string)v, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) return parsed;
			return fallback;
		}

		public static int Int(Dictionary<string, object> d, string key, int fallback = 0) { return (int)Num(d, key, fallback); }

		public static bool Bool(Dictionary<string, object> d, string key, bool fallback = false)
		{
			object v;
			if (d == null || !d.TryGetValue(key, out v) || !(v is bool)) return fallback;
			return (bool)v;
		}

		public static ulong Ulong(Dictionary<string, object> d, string key)
		{
			object v;
			if (d == null || !d.TryGetValue(key, out v) || v == null) return 0;
			ulong r;
			if (v is string && ulong.TryParse((string)v, out r)) return r;
			if (v is double) return (ulong)(double)v;
			return 0;
		}

		public static Dictionary<string, object> Map(Dictionary<string, object> d, string key)
		{
			object v;
			return d != null && d.TryGetValue(key, out v) ? v as Dictionary<string, object> : null;
		}

		public static List<object> Arr(Dictionary<string, object> d, string key)
		{
			object v;
			return d != null && d.TryGetValue(key, out v) ? v as List<object> : null;
		}

		static void SkipWs(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

		static object ReadValue(string s, ref int i)
		{
			SkipWs(s, ref i);
			char c = s[i];
			if (c == '{')
			{
				var d = new Dictionary<string, object>();
				i++; SkipWs(s, ref i);
				if (s[i] == '}') { i++; return d; }
				while (true)
				{
					SkipWs(s, ref i);
					var k = ReadString(s, ref i);
					SkipWs(s, ref i); i++;
					d[k] = ReadValue(s, ref i);
					SkipWs(s, ref i);
					if (s[i] == ',') { i++; continue; }
					i++; return d;
				}
			}
			if (c == '[')
			{
				var l = new List<object>();
				i++; SkipWs(s, ref i);
				if (s[i] == ']') { i++; return l; }
				while (true)
				{
					l.Add(ReadValue(s, ref i));
					SkipWs(s, ref i);
					if (s[i] == ',') { i++; continue; }
					i++; return l;
				}
			}
			if (c == '"') return ReadString(s, ref i);
			if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
			if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
			if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }

			int start = i;
			while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
			return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
		}

		static string ReadString(string s, ref int i)
		{
			i++;
			var sb = new StringBuilder();
			while (s[i] != '"')
			{
				if (s[i] == '\\')
				{
					i++;
					switch (s[i])
					{
						case 'n': sb.Append('\n'); break;
						case 'r': sb.Append('\r'); break;
						case 't': sb.Append('\t'); break;
						case 'b': sb.Append('\b'); break;
						case 'f': sb.Append('\f'); break;
						case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
						default: sb.Append(s[i]); break;
					}
				}
				else sb.Append(s[i]);
				i++;
			}
			i++;
			return sb.ToString();
		}
	}
}
