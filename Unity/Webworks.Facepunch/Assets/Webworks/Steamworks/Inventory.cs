#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.Internal;

namespace Steamworks.Data
{
	public class InventoryRecipe
	{
		public struct Ingredient { public int DefinitionId; public InventoryDef Definition; public int Count; }
		public InventoryDef Result;
		public Ingredient[] Ingredients = new Ingredient[0];
		public InventoryDef Source;
	}

	public class InventoryDef
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Type { get; set; }
		public string IconUrl { get; set; }
		public string IconUrlLarge { get; set; }
		public string ExchangeSchema { get; set; }
		public string PriceCategory { get; set; }
		public bool Marketable { get; set; }
		public bool Tradable { get; set; }
		public bool IsGenerator { get; set; }
		public bool ClientGrantable { get; set; }
		public DateTime Created { get; set; }
		public DateTime Modified { get; set; }
		public double LocalPrice { get; set; }
		public string LocalPriceFormatted { get; set; }
		public double LocalBasePrice { get; set; }
		public string LocalBasePriceFormatted { get; set; }
		public Dictionary<string, string> Properties = new Dictionary<string, string>();

		public string GetProperty(string name) { string v; return Properties.TryGetValue(name, out v) ? v : null; }
		public bool GetBoolProperty(string name) { var v = GetProperty(name); return v == "true" || v == "1"; }
		public InventoryRecipe[] GetRecipes() { return new InventoryRecipe[0]; }
		public InventoryRecipe[] GetRecipesContainingThis() { return new InventoryRecipe[0]; }
	}

	public struct InventoryItem
	{
		public struct Amount { public InventoryItem Item; public int Quantity; }

		public InventoryItemId Id;
		public InventoryDefId DefId;
		public InventoryDef Def { get { return SteamInventory.FindDefinition(DefId); } }
		public int Quantity;
		public DateTime Acquired;
		public string Origin;
		public Dictionary<string, string> Properties;
		public bool IsNoTrade { get { return false; } }
		public bool IsRemoved { get { return false; } }
		public bool IsConsumed { get { return false; } }

		public Task<InventoryResult> ConsumeAsync(int amount = 1) { return SteamInventory.ConsumeAsync(this, amount); }
		public Task<InventoryResult> SplitStackAsync(int quantity = 1) { return Task.FromResult<InventoryResult>(null); }
		public Task<InventoryResult> AddAsync(int quantity = 1) { return SteamInventory.GenerateAsync(DefId, quantity); }
	}

	public class InventoryResult : IDisposable
	{
		internal InventoryItem[] _items = new InventoryItem[0];

		public int ItemCount { get { return _items.Length; } }
		public bool Expired { get { return false; } }
		public bool BelongsTo(SteamId steamId) { return true; }
		public InventoryItem[] GetItems(bool includeProperties = false) { return _items; }
		public byte[] Serialize()
		{
			var sb = new StringBuilder("[");
			for (int i = 0; i < _items.Length; i++)
			{
				if (i > 0) sb.Append(',');
				sb.Append("{\"i\":").Append(_items[i].Id.Value)
					.Append(",\"d\":").Append(_items[i].DefId.Value)
					.Append(",\"q\":").Append(_items[i].Quantity).Append('}');
			}
			sb.Append(']');
			return Encoding.UTF8.GetBytes(sb.ToString());
		}
		public void Dispose() { }
	}
}

namespace Steamworks
{
	using Steamworks.Data;
	using System.Linq;

	public static class SteamInventory
	{
		public static event Action<InventoryResult> OnInventoryUpdated;
		public static event Action OnDefinitionsUpdated;

		static readonly Dictionary<int, InventoryDef> _defs = new Dictionary<int, InventoryDef>();
		static readonly Dictionary<string, int> _defIds = new Dictionary<string, int>();
		static readonly Dictionary<ulong, InventoryItem> _items = new Dictionary<ulong, InventoryItem>();
		static InventoryItem[] _itemList = new InventoryItem[0];
		static int _nextInstance = 1;
		static Task _loading;

		public static string Currency { get { return "USD"; } }

		public static InventoryDef[] Definitions { get { return _defs.Values.ToArray(); } }

		public static InventoryItem[] Items { get { return _itemList; } }

		public static InventoryDef FindDefinition(InventoryDefId defId)
		{
			InventoryDef def;
			return _defs.TryGetValue(defId.Value, out def) ? def : null;
		}

		internal static int IdFor(string defId)
		{
			int id;
			if (_defIds.TryGetValue(defId, out id)) return id;
			id = defId.GetHashCode() & 0x7fffffff;
			if (id == 0) id = _defIds.Count + 1;
			while (_defs.ContainsKey(id)) id++;
			_defIds[defId] = id;
			return id;
		}

		public static bool LoadItemDefinitions()
		{
			Refresh().Forget("inventory definitions");
			return true;
		}

		public static Task<bool> WaitForDefinitions(float timeoutSeconds = 30f)
		{
			return Task.FromResult(Refresh().Wait((int)(timeoutSeconds * 1000)));
		}

		static async Task<bool> Refresh()
		{
			if (_loading != null) { await _loading; return _defs.Count > 0; }
			_loading = RefreshAsync();
			await _loading;
			_loading = null;
			return _defs.Count > 0;
		}

		static async Task RefreshAsync()
		{
			var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Inventory/Definitions"));
			var entries = payload != null ? WebworksJson.Arr(payload, "definitions") : null;

			_defs.Clear();
			_defIds.Clear();
			if (entries != null)
			{
				foreach (var raw in entries)
				{
					var entry = raw as Dictionary<string, object>;
					if (entry == null) continue;
					var defId = WebworksJson.Str(entry, "itemDefId") ?? "";
					var def = new InventoryDef
					{
						Id = IdFor(defId),
						Name = WebworksJson.Str(entry, "name") ?? defId,
						Description = WebworksJson.Str(entry, "description") ?? "",
						Type = WebworksJson.Str(entry, "type") ?? "item",
						IconUrl = WebworksJson.Str(entry, "iconUrl") ?? "",
						Tradable = WebworksJson.Bool(entry, "tradable"),
						Marketable = WebworksJson.Bool(entry, "marketable"),
						ClientGrantable = WebworksJson.Bool(entry, "clientGrantable"),
						Properties = new Dictionary<string, string>
						{
							{ "name", WebworksJson.Str(entry, "name") ?? defId },
							{ "type", WebworksJson.Str(entry, "type") ?? "item" },
						},
					};
					_defs[def.Id] = def;
				}
			}

			OnDefinitionsUpdated?.Invoke();
			await RefreshItemsAsync();
		}

		static async Task RefreshItemsAsync()
		{
			var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Inventory/Items"));
			var entries = payload != null ? WebworksJson.Arr(payload, "items") : null;

			_items.Clear();
			var list = new List<InventoryItem>();
			if (entries != null)
			{
				foreach (var raw in entries)
				{
					var entry = raw as Dictionary<string, object>;
					if (entry == null) continue;

					var defId = WebworksJson.Str(entry, "itemDefId") ?? "";
					var quantity = Math.Max(0, WebworksJson.Int(entry, "quantity"));
					var id = IdFor(defId);

					var item = new InventoryItem
					{
						Id = new InventoryItemId((ulong)id * 100000UL + (ulong)(_nextInstance++)),
						DefId = new InventoryDefId(id),
						Quantity = quantity,
						Acquired = DateTime.UtcNow,
						Origin = "webworks",
						Properties = new Dictionary<string, string>(),
					};
					_items[item.Id.Value] = item;
					list.Add(item);
				}
			}

			_itemList = list.ToArray();
			OnInventoryUpdated?.Invoke(new InventoryResult { _items = _itemList });
		}

		public static bool GetAllItems()
		{
			ApiClient.Fire(async () => { await Refresh(); return ""; }, "GetAllItems");
			return true;
		}

		public static Task<InventoryResult> GetAllItemsAsync()
		{
			return Task.Run(async () =>
			{
				await Refresh();
				return new InventoryResult { _items = _itemList };
			});
		}

		public static Task<InventoryDef[]> GetDefinitionsWithPricesAsync()
		{
			return Task.FromResult(Definitions);
		}

		internal static async Task<InventoryResult> ConsumeAsync(InventoryItem item, int amount)
		{
			var def = FindDefinition(item.DefId);
			if (def == null) return null;

			var defId = DefIdString(item.DefId);
			var body = "{\"itemDefId\":" + WebworksJson.Quote(defId) + ",\"quantity\":" + Math.Max(1, amount) + "}";
			var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Inventory/Consume", body));
			if (payload == null || WebworksJson.Str(payload, "error") != null) return null;

			await Refresh();
			return new InventoryResult { _items = _itemList };
		}

		internal static string DefIdString(InventoryDefId defId)
		{
			foreach (var kv in _defIds) if (kv.Value == defId.Value) return kv.Key;
			return defId.Value.ToString(CultureInfo.InvariantCulture);
		}

		internal static async Task<InventoryResult> GenerateAsync(InventoryDefId defId, int amount)
		{
			var body = "{\"itemDefId\":" + WebworksJson.Quote(DefIdString(defId)) + ",\"quantity\":" + Math.Max(1, amount) + "}";
			var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Inventory/GrantSelf", body));
			if (payload == null || WebworksJson.Str(payload, "error") != null)
			{
				Debug.LogWarning("[Webworks] this item definition is not client-grantable; grant it from the game server instead.");
				return null;
			}

			await Refresh();
			return new InventoryResult { _items = _itemList };
		}

		public static Task<InventoryResult> GenerateItemAsync(InventoryDef target, int amount)
		{
			if (target == null) return Task.FromResult<InventoryResult>(null);
			return GenerateAsync(new InventoryDefId(target.Id), amount);
		}

		public static Task<InventoryResult> GrantPromoItemsAsync() { return GetAllItemsAsync(); }
		public static Task<InventoryResult> AddPromoItemAsync(InventoryDefId defId) { return GenerateAsync(defId, 1); }
		public static Task<InventoryResult> TriggerItemDropAsync(InventoryDefId defId) { return GenerateAsync(defId, 1); }

		public static async Task<InventoryResult> CraftItemAsync(InventoryItem.Amount[] list, InventoryDef target)
		{
			if (list == null || target == null) return null;
			foreach (var entry in list)
			{
				var result = await ConsumeAsync(entry.Item, entry.Quantity);
				if (result == null) return null;
			}
			return await GenerateAsync(new InventoryDefId(target.Id), 1);
		}

		public static Task<InventoryResult> DeserializeAsync(byte[] data, int dataLength = -1)
		{
			if (data == null) return Task.FromResult<InventoryResult>(null);
			try
			{
				var text = Encoding.UTF8.GetString(data, 0, dataLength < 0 ? data.Length : Math.Min(dataLength, data.Length));
				var items = new List<InventoryItem>();
				int index = 0;
				while (true)
				{
					int start = text.IndexOf("{\"i\":", index, StringComparison.Ordinal);
					if (start < 0) break;
					int stop = text.IndexOf('}', start);
					if (stop < 0) break;

					ulong instance = 0;
					int def = 0;
					int quantity = 1;
					foreach (var part in text.Substring(start + 1, stop - start - 1).Split(','))
					{
						var kv = part.Split(':');
						if (kv.Length != 2) continue;
						var key = kv[0].Trim('"');
						uint value;
						uint.TryParse(kv[1], out value);
						if (key == "i") instance = value;
						else if (key == "d") def = (int)value;
						else if (key == "q") quantity = (int)value;
					}

					items.Add(new InventoryItem
					{
						Id = new InventoryItemId(instance),
						DefId = new InventoryDefId(def),
						Quantity = quantity,
						Properties = new Dictionary<string, string>(),
					});
					index = stop;
				}
				return Task.FromResult(new InventoryResult { _items = items.ToArray() });
			}
			catch
			{
				return Task.FromResult<InventoryResult>(null);
			}
		}

		public static Task<InventoryPurchaseResult?> StartPurchaseAsync(InventoryDef[] items)
		{
			Debug.LogWarning("[Webworks] purchases are not supported: Webworks has no store or wallet. Grant items from the game server instead.");
			return Task.FromResult<InventoryPurchaseResult?>(null);
		}
	}
}
