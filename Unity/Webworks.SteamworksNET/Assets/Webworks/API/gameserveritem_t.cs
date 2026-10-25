



using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;

using System.Text;

namespace Steamworks {
	[StructLayout(LayoutKind.Sequential, Size = 372, Pack = 4)]
	[System.Serializable]
	public class gameserveritem_t {
		public string GetGameDir() {
			return Encoding.UTF8.GetString(m_szGameDir, 0, System.Array.IndexOf<byte>(m_szGameDir, 0));
		}

		public void SetGameDir(string dir) {
			m_szGameDir = Encoding.UTF8.GetBytes(dir + '\0');
		}

		public string GetMap() {
			return Encoding.UTF8.GetString(m_szMap, 0, System.Array.IndexOf<byte>(m_szMap, 0));
		}

		public void SetMap(string map) {
			m_szMap = Encoding.UTF8.GetBytes(map + '\0');
		}

		public string GetGameDescription() {
			return Encoding.UTF8.GetString(m_szGameDescription, 0, System.Array.IndexOf<byte>(m_szGameDescription, 0));
		}

		public void SetGameDescription(string desc) {
			m_szGameDescription = Encoding.UTF8.GetBytes(desc + '\0');
		}

		public string GetServerName() {
			if (m_szServerName[0] == 0)
				return m_NetAdr.GetConnectionAddressString();
			else
				return Encoding.UTF8.GetString(m_szServerName, 0, System.Array.IndexOf<byte>(m_szServerName, 0));
		}

		public void SetServerName(string name) {
			m_szServerName = Encoding.UTF8.GetBytes(name + '\0');
		}

		public string GetGameTags() {
			return Encoding.UTF8.GetString(m_szGameTags, 0, System.Array.IndexOf<byte>(m_szGameTags, 0));
		}

		public void SetGameTags(string tags) {
			m_szGameTags = Encoding.UTF8.GetBytes(tags + '\0');
		}

		public servernetadr_t m_NetAdr;
		public int m_nPing;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHadSuccessfulResponse;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bDoNotRefresh;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cbMaxGameServerGameDir)]
		private byte[] m_szGameDir;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cbMaxGameServerMapName)]
		private byte[] m_szMap;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cbMaxGameServerGameDescription)]
		private byte[] m_szGameDescription;
		public uint m_nAppID;
		public int m_nPlayers;
		public int m_nMaxPlayers;
		public int m_nBotPlayers;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bPassword;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bSecure;
		public uint m_ulTimeLastPlayed;
		public int	m_nServerVersion;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cbMaxGameServerName)]
		private byte[] m_szServerName;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cbMaxGameServerTags)]
		private byte[] m_szGameTags;

		public CSteamID m_steamID;
	}
}

