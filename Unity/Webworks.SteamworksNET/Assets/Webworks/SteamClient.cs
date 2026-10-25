using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steamworks
{
	// The pipe/user plumbing is real here: handles exist, are tracked and are released.
	// Interface pointers are stable non-zero sentinels, because the shim exposes every
	// interface through the static classes rather than through native vtables.
	public static partial class SteamClient
	{
		static readonly HashSet<uint> _pipes = new HashSet<uint>();
		static readonly HashSet<uint> _users = new HashSet<uint>();
		static uint _nextPipe = 1;
		static uint _nextUser = 1;
		static ulong _ipcCalls;
		static readonly Dictionary<string, IntPtr> _interfacePointers = new Dictionary<string, IntPtr>();
		static int _nextInterface = 1;

		static IntPtr InterfaceFor(string version)
		{
			var key = version ?? "";
			IntPtr pointer;
			if (_interfacePointers.TryGetValue(key, out pointer)) return pointer;
			pointer = new IntPtr(0x1000 + _nextInterface++);
			_interfacePointers[key] = pointer;
			return pointer;
		}

		public static HSteamPipe CreateSteamPipe()
		{
			var id = _nextPipe++;
			_pipes.Add(id);
			return new HSteamPipe((int)id);
		}

		public static bool BReleaseSteamPipe(HSteamPipe hSteamPipe)
		{
			return _pipes.Remove((uint)hSteamPipe.m_HSteamPipe);
		}

		public static HSteamUser ConnectToGlobalUser(HSteamPipe hSteamPipe)
		{
			if (!_pipes.Contains((uint)hSteamPipe.m_HSteamPipe)) return new HSteamUser(0);
			var id = _nextUser++;
			_users.Add(id);
			return new HSteamUser((int)id);
		}

		public static HSteamUser CreateLocalUser(out HSteamPipe phSteamPipe, EAccountType eAccountType)
		{
			phSteamPipe = CreateSteamPipe();
			var id = _nextUser++;
			_users.Add(id);
			return new HSteamUser((int)id);
		}

		public static void ReleaseUser(HSteamPipe hSteamPipe, HSteamUser hUser)
		{
			_users.Remove((uint)hUser.m_HSteamUser);
		}

		public static IntPtr GetISteamUser(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("user:" + pchVersion); }
		public static IntPtr GetISteamGameServer(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("gameserver:" + pchVersion); }

		public static void SetLocalIPBinding(ref SteamIPAddress_t unIP, ushort usPort)
		{
			// Every connection leaves through the server's websocket relay, so a local bind
			// address has no meaning; remember it for GetPublicIP style callers.
			_localPort = usPort;
		}

		static ushort _localPort;

		internal static ushort LocalPort { get { return _localPort; } }

		public static IntPtr GetISteamFriends(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("friends:" + pchVersion); }
		public static IntPtr GetISteamUtils(HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("utils:" + pchVersion); }
		public static IntPtr GetISteamMatchmaking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("matchmaking:" + pchVersion); }
		public static IntPtr GetISteamMatchmakingServers(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("matchmakingservers:" + pchVersion); }
		public static IntPtr GetISteamGenericInterface(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("generic:" + pchVersion); }
		public static IntPtr GetISteamUserStats(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("userstats:" + pchVersion); }
		public static IntPtr GetISteamGameServerStats(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("gameserverstats:" + pchVersion); }
		public static IntPtr GetISteamApps(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("apps:" + pchVersion); }
		public static IntPtr GetISteamNetworking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("networking:" + pchVersion); }
		public static IntPtr GetISteamRemoteStorage(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("remotestorage:" + pchVersion); }
		public static IntPtr GetISteamScreenshots(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("screenshots:" + pchVersion); }

		public static uint GetIPCCallCount()
		{
			return (uint)(_ipcCalls > uint.MaxValue ? uint.MaxValue : _ipcCalls);
		}

		internal static void CountIpcCall() { _ipcCalls++; }

		internal static void Warning(string message)
		{
			if (_hook != null)
			{
				try
				{
					_hook(1, new System.Text.StringBuilder(message));
					return;
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[Webworks] warning hook threw: " + ex.Message);
				}
			}
			Debug.LogWarning("[Webworks] " + message);
		}

		public static bool BShutdownIfAllPipesClosed()
		{
			return _pipes.Count == 0;
		}

		public static IntPtr GetISteamHTTP(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("http:" + pchVersion); }
		public static IntPtr GetISteamController(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("controller:" + pchVersion); }
		public static IntPtr GetISteamUGC(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("ugc:" + pchVersion); }
		public static IntPtr GetISteamMusic(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("music:" + pchVersion); }
		public static IntPtr GetISteamHTMLSurface(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("htmlsurface:" + pchVersion); }
		public static IntPtr GetISteamInventory(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("inventory:" + pchVersion); }
		public static IntPtr GetISteamVideo(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("video:" + pchVersion); }
		public static IntPtr GetISteamParentalSettings(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("parental:" + pchVersion); }
		public static IntPtr GetISteamInput(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("input:" + pchVersion); }
		public static IntPtr GetISteamParties(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("parties:" + pchVersion); }
		public static IntPtr GetISteamRemotePlay(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion) { return InterfaceFor("remoteplay:" + pchVersion); }

		internal static void ResetHandles()
		{
			_pipes.Clear();
			_users.Clear();
			_interfacePointers.Clear();
			_ipcCalls = 0;
		}
	}
}
