using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamMatchmaking
	{
		class Favorite
		{
			public uint AppId;
			public uint Ip;
			public ushort ConnPort;
			public ushort QueryPort;
			public uint Flags;
			public uint LastPlayed;
		}

		static readonly List<Favorite> _favorites = new List<Favorite>();
		static bool _favoritesLoaded;
		static string _compatibleLobby = "";
		const string FavoritesKey = "webworks.matchmaking.favorites";

		static void LoadFavorites()
		{
			if (_favoritesLoaded) return;
			_favoritesLoaded = true;
			try
			{
				var raw = PlayerPrefs.GetString(FavoritesKey, "");
				if (string.IsNullOrEmpty(raw)) return;
				foreach (var row in raw.Split(';'))
				{
					var parts = row.Split(',');
					if (parts.Length < 6) continue;
					_favorites.Add(new Favorite
					{
						AppId = uint.Parse(parts[0]),
						Ip = uint.Parse(parts[1]),
						ConnPort = ushort.Parse(parts[2]),
						QueryPort = ushort.Parse(parts[3]),
						Flags = uint.Parse(parts[4]),
						LastPlayed = uint.Parse(parts[5]),
					});
				}
			}
			catch
			{
				_favorites.Clear();
			}
		}

		static void SaveFavorites()
		{
			var sb = new StringBuilder();
			foreach (var f in _favorites)
			{
				if (sb.Length > 0) sb.Append(';');
				sb.Append(f.AppId).Append(',').Append(f.Ip).Append(',').Append(f.ConnPort).Append(',')
					.Append(f.QueryPort).Append(',').Append(f.Flags).Append(',').Append(f.LastPlayed);
			}
			try
			{
				PlayerPrefs.SetString(FavoritesKey, sb.ToString());
				PlayerPrefs.Save();
			}
			catch
			{
			}
		}

		public static int GetFavoriteGameCount()
		{
			LoadFavorites();
			return _favorites.Count;
		}

		public static bool GetFavoriteGame(int iGame, out AppId_t pnAppID, out uint pnIP, out ushort pnConnPort, out ushort pnQueryPort, out uint punFlags, out uint pRTime32LastPlayedOnServer)
		{
			LoadFavorites();
			if (iGame < 0 || iGame >= _favorites.Count)
			{
				pnAppID = AppId_t.Invalid;
				pnIP = 0;
				pnConnPort = 0;
				pnQueryPort = 0;
				punFlags = 0;
				pRTime32LastPlayedOnServer = 0;
				return false;
			}

			var f = _favorites[iGame];
			pnAppID = new AppId_t(f.AppId);
			pnIP = f.Ip;
			pnConnPort = f.ConnPort;
			pnQueryPort = f.QueryPort;
			punFlags = f.Flags;
			pRTime32LastPlayedOnServer = f.LastPlayed;
			return true;
		}

		public static int AddFavoriteGame(AppId_t nAppID, uint nIP, ushort nConnPort, ushort nQueryPort, uint unFlags, uint rTime32LastPlayedOnServer)
		{
			LoadFavorites();
			for (int i = 0; i < _favorites.Count; i++)
			{
				var existing = _favorites[i];
				if (existing.AppId != nAppID.m_AppId || existing.Ip != nIP || existing.ConnPort != nConnPort) continue;
				existing.QueryPort = nQueryPort;
				existing.Flags = unFlags;
				existing.LastPlayed = rTime32LastPlayedOnServer;
				SaveFavorites();
				return i + 1;
			}

			_favorites.Add(new Favorite
			{
				AppId = nAppID.m_AppId,
				Ip = nIP,
				ConnPort = nConnPort,
				QueryPort = nQueryPort,
				Flags = unFlags,
				LastPlayed = rTime32LastPlayedOnServer,
			});
			SaveFavorites();
			return _favorites.Count;
		}

		public static bool RemoveFavoriteGame(AppId_t nAppID, uint nIP, ushort nConnPort, ushort nQueryPort, uint unFlags)
		{
			LoadFavorites();
			var removed = _favorites.RemoveAll(f => f.AppId == nAppID.m_AppId && f.Ip == nIP && f.ConnPort == nConnPort) > 0;
			if (removed) SaveFavorites();
			return removed;
		}

		public static void AddRequestLobbyListCompatibleMembersFilter(CSteamID steamIDLobby)
		{
			_compatibleLobby = steamIDLobby.m_SteamID.ToString();
		}

		public static bool SetLobbyOwner(CSteamID steamIDLobby, CSteamID steamIDNewOwner)
		{
			ApiClient.Fire(async () =>
			{
				await ApiClient.PostAsync("/Matchmaking/LobbyOwner", "{\"lobbyId\":" + WebworksJson.Quote(steamIDLobby.m_SteamID.ToString())
					+ ",\"ownerId\":" + WebworksJson.Quote(steamIDNewOwner.m_SteamID.ToString()) + "}");
				return (string)null;
			}, "SetLobbyOwner");
			return true;
		}

		public static bool SetLinkedLobby(CSteamID steamIDLobby, CSteamID steamIDLobbyDependent)
		{
			ApiClient.Fire(async () =>
			{
				await ApiClient.PostAsync("/Matchmaking/LobbyLinked", "{\"lobbyId\":" + WebworksJson.Quote(steamIDLobby.m_SteamID.ToString())
					+ ",\"linkedLobbyId\":" + WebworksJson.Quote(steamIDLobbyDependent.m_SteamID.ToString()) + "}");
				return (string)null;
			}, "SetLinkedLobby");
			return true;
		}
	}
}
