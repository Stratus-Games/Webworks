using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Steamworks.ServerList
{
	public static class LocalServerLists
	{
		public static readonly IdSet Favourites = new IdSet("webworks_server_favourites");
		public static readonly IdSet History = new IdSet("webworks_server_history");

		public class IdSet
		{
			readonly string _key;
			HashSet<ulong> _ids;

			public IdSet(string key) { _key = key; }

			void EnsureLoaded()
			{
				if (_ids != null) return;
				var raw = PlayerPrefs.HasKey(_key) ? PlayerPrefs.GetString(_key) : "";
				_ids = new HashSet<ulong>(raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => { ulong.TryParse(s, out var v); return v; })
					.Where(v => v != 0));
			}

			void Save()
			{
				PlayerPrefs.SetString(_key, string.Join(",", _ids));
				PlayerPrefs.Save();
			}

			public bool Contains(ulong id) { EnsureLoaded(); return _ids.Contains(id); }
			public void Add(ulong id) { EnsureLoaded(); if (_ids.Add(id)) Save(); }
			public void Remove(ulong id) { EnsureLoaded(); if (_ids.Remove(id)) Save(); }
			public IEnumerable<ulong> All() { EnsureLoaded(); return _ids; }
		}
	}
}
