using System;
using System.Text;

namespace Steamworks
{
	

	public struct SteamNetworkingIdentity : IEquatable<SteamNetworkingIdentity>
	{
		public ESteamNetworkingIdentityType m_eType;
		public int m_cbSize;
		ulong _steamId;
		string _generic;

		public void Clear() { m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_Invalid; m_cbSize = 0; _steamId = 0; _generic = null; }
		public bool IsInvalid() { return m_eType == ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_Invalid; }

		public void SetSteamID(CSteamID steamID) { SetSteamID64(steamID.m_SteamID); }
		public CSteamID GetSteamID() { return m_eType == ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID ? new CSteamID(_steamId) : CSteamID.Nil; }
		public void SetSteamID64(ulong steamID) { m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID; m_cbSize = 8; _steamId = steamID; }
		public ulong GetSteamID64() { return m_eType == ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID ? _steamId : 0; }

		public bool SetGenericString(string pszString)
		{
			if (string.IsNullOrEmpty(pszString) || pszString.Length >= 32) return false;
			m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_GenericString; m_cbSize = pszString.Length + 1; _generic = pszString; return true;
		}
		public string GetGenericString() { return m_eType == ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_GenericString ? _generic : null; }

		public bool SetGenericBytes(byte[] data, uint cbLen)
		{
			if (data == null || cbLen == 0 || cbLen > 32) return false;
			m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_GenericBytes; m_cbSize = (int)cbLen; _generic = Convert.ToBase64String(data, 0, (int)cbLen); return true;
		}
		public byte[] GetGenericBytes(out int cbLen)
		{
			if (m_eType != ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_GenericBytes) { cbLen = 0; return null; }
			var b = Convert.FromBase64String(_generic); cbLen = b.Length; return b;
		}

		public void SetLocalHost() { m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_IPAddress; m_cbSize = 18; _generic = "127.0.0.1"; }
		public bool IsLocalHost() { return m_eType == ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_IPAddress && _generic == "127.0.0.1"; }

		public override string ToString()
		{
			switch (m_eType)
			{
				case ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID: return "steamid:" + _steamId;
				case ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_GenericString: return "str:" + _generic;
				case ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_GenericBytes: return "gen:" + _generic;
				case ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_IPAddress: return "ip:" + _generic;
				default: return "";
			}
		}
		public void ToString(out string buf) { buf = ToString(); }
		public bool ParseString(string pszStr)
		{
			Clear();
			if (string.IsNullOrEmpty(pszStr)) return false;
			ulong id;
			if (pszStr.StartsWith("steamid:") && ulong.TryParse(pszStr.Substring(8), out id)) { SetSteamID64(id); return true; }
			if (pszStr.StartsWith("str:")) return SetGenericString(pszStr.Substring(4));
			return false;
		}

		public bool Equals(SteamNetworkingIdentity o) { return m_eType == o.m_eType && _steamId == o._steamId && _generic == o._generic; }
		public override bool Equals(object o) { return o is SteamNetworkingIdentity && Equals((SteamNetworkingIdentity)o); }
		public override int GetHashCode() { return (int)m_eType ^ _steamId.GetHashCode() ^ (_generic != null ? _generic.GetHashCode() : 0); }
		public static bool operator ==(SteamNetworkingIdentity a, SteamNetworkingIdentity b) { return a.Equals(b); }
		public static bool operator !=(SteamNetworkingIdentity a, SteamNetworkingIdentity b) { return !a.Equals(b); }
	}
}

public static class HttpUtility
{
	public static string UrlEncode(string s) { return s == null ? null : Uri.EscapeDataString(s).Replace("%20", "+"); }
	public static string UrlDecode(string s) { return s == null ? null : Uri.UnescapeDataString(s.Replace("+", " ")); }
	public static string UrlPathEncode(string s) { return s == null ? null : Uri.EscapeUriString(s); }
	public static string HtmlEncode(string s) { return s == null ? null : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;"); }
	public static string HtmlDecode(string s) { return s == null ? null : s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&amp;", "&"); }
	public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query)
	{
		var nv = new System.Collections.Specialized.NameValueCollection();
		if (string.IsNullOrEmpty(query)) return nv;
		if (query[0] == '?') query = query.Substring(1);
		foreach (var part in query.Split('&'))
		{
			if (part.Length == 0) continue;
			int i = part.IndexOf('=');
			if (i < 0) nv.Add(null, UrlDecode(part));
			else nv.Add(UrlDecode(part.Substring(0, i)), UrlDecode(part.Substring(i + 1)));
		}
		return nv;
	}
}
