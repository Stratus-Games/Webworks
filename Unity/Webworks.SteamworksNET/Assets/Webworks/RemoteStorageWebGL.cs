#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Browser builds have no persistent filesystem, and browser storage is what the player
	// already saves into. SteamRemoteStorage files therefore go to their own IndexedDB
	// database, "WEBWORKS-SAVES", which is separate from the game's own saves so the two
	// never collide. No server round trip is involved.
	public static partial class SteamRemoteStorage
	{
		static bool _webStoreStarted;

		internal static void InitWebStore()
		{
			if (_webStoreStarted) return;
			_webStoreStarted = true;
			try { WebworksNative.Webworks_Saves_Init(); }
			catch (Exception ex) { Debug.LogWarning("[Webworks] WEBWORKS-SAVES could not be opened: " + ex.Message); }
		}

		// IndexedDB is async, so the initial file list and any writes made before the
		// database opened are picked up on the next frame from RunCallbacks.
		internal static void PollWebStore()
		{
			if (!_webStoreStarted) return;

			string json;
			try { json = WebworksNative.Webworks_Saves_Take(); }
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] could not read WEBWORKS-SAVES: " + ex.Message);
				_webStoreStarted = false;
				return;
			}

			if (!string.IsNullOrEmpty(json)) LoadFromJson(json);
		}

		static void FlushWebStore()
		{
			if (!_webStoreStarted) InitWebStore();
			while (_pending.Count > 0)
			{
				string name = null;
				foreach (var key in _pending.Keys) { name = key; break; }
				if (name == null) break;

				byte[] data = _pending[name];
				_pending.Remove(name);

				try
				{
					if (data == null) WebworksNative.Webworks_Saves_Delete(name);
					else WebworksNative.Webworks_Saves_Write(name, Convert.ToBase64String(data));
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[Webworks] could not write '" + name + "' to WEBWORKS-SAVES: " + ex.Message);
					_pending[name] = data;
					break;
				}
			}
		}
	}
}
#endif
