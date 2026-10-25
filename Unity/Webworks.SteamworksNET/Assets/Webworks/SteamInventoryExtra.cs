using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Generation and trading on top of the inventory routes. Item definitions and the
	// owned-items list are already loaded by SteamInventory.cs, so these methods only add
	// the mutations and their result handles.
	public static partial class SteamInventory
	{
		static readonly Dictionary<ulong, Dictionary<string, string>> _stagedProperties = new Dictionary<ulong, Dictionary<string, string>>();
		static ulong _nextPropertyHandle = 1;
		static bool _pricesRequested;

		static Definition DefinitionOf(SteamItemDef_t itemDef)
		{
			Definition def;
			return _defsById.TryGetValue(itemDef.m_SteamItemDef, out def) ? def : null;
		}

		static Definition DefinitionOf(ulong instance)
		{
			string defId;
			if (!_instanceDef.TryGetValue(instance, out defId)) return null;
			int def;
			return _idsByDef.TryGetValue(defId, out def) && _defsById.TryGetValue(def, out Definition known) ? known : null;
		}

		static string ItemsJson(SteamItemDef_t[] defs, uint[] quantities, uint length)
		{
			var body = new StringBuilder("[");
			if (defs != null)
				for (uint i = 0; i < length && i < defs.Length; i++)
				{
					var def = DefinitionOf(defs[i]);
					if (def == null) return null;
					uint quantity = quantities != null && i < quantities.Length ? quantities[i] : 1;
					if (i > 0) body.Append(',');
					body.Append("{\"itemDefId\":").Append(WebworksJson.Quote(def.DefId))
						.Append(",\"quantity\":").Append(Math.Max(1, quantity).ToString(CultureInfo.InvariantCulture)).Append('}');
				}
			body.Append(']');
			return body.ToString();
		}

		static List<SteamItemDetails_t> ParseItems(Dictionary<string, object> payload)
		{
			var items = new List<SteamItemDetails_t>();
			var entries = payload != null ? WebworksJson.Arr(payload, "items") : null;
			_instanceDef.Clear();
			if (entries == null) return items;

			foreach (var raw in entries)
			{
				var entry = raw as Dictionary<string, object>;
				if (entry == null) continue;
				var defId = WebworksJson.Str(entry, "itemDefId") ?? "";
				var quantity = Math.Max(0, WebworksJson.Int(entry, "quantity"));
				var definitionId = IdFor(defId);
				for (int i = 0; i < quantity; i++)
				{
					ulong instance = (ulong)definitionId * 100000UL + (ulong)(_nextInstance++);
					_instanceDef[instance] = defId;
					items.Add(new SteamItemDetails_t
					{
						m_itemId = new SteamItemInstanceID_t(instance),
						m_iDefinition = new SteamItemDef_t(definitionId),
						m_unQuantity = 1,
						m_unFlags = 0,
					});
				}
			}
			return items;
		}

		static void RefreshItems(Action<List<SteamItemDetails_t>, EResult> done)
		{
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Inventory/Items"));
				var items = ParseItems(payload);
				if (payload != null) _lastAllItems = items;
				done(items, payload != null ? EResult.k_EResultOK : EResult.k_EResultFail);
				return "";
			}, "InventoryRefresh");
		}

		static void CompleteResult(List<SteamItemDetails_t> items, EResult result)
		{
			CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
			{
				m_handle = new SteamInventoryResult_t(NewResult(items ?? new List<SteamItemDetails_t>())),
				m_result = result,
			});
		}

		static bool PostThenRefresh(string path, string body, out SteamInventoryResult_t handle, string what)
		{
			handle = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync(path, body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				if (!ok)
				{
					CompleteResult(new List<SteamItemDetails_t>(), EResult.k_EResultFail);
					return "";
				}
				RefreshItems((items, result) => CompleteResult(items, result));
				return "";
			}, what);
			return true;
		}

		public static bool GenerateItems(out SteamInventoryResult_t pResultHandle, SteamItemDef_t[] pArrayItemDefs, uint[] punArrayQuantity, uint unArrayLength)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;
			var items = ItemsJson(pArrayItemDefs, punArrayQuantity, unArrayLength);
			if (items == null) return false;
			return PostThenRefresh("/Inventory/GrantSelf", "{\"items\":" + items + "}", out pResultHandle, "GenerateItems");
		}

		public static bool GrantPromoItems(out SteamInventoryResult_t pResultHandle)
		{
			if (!_definitionsLoaded) LoadItemDefinitions();

			var body = new StringBuilder("[");
			var first = true;
			foreach (var def in _defsById.Values)
			{
				if (!def.Promo || !def.ClientGrantable) continue;
				if (!first) body.Append(',');
				first = false;
				body.Append("{\"itemDefId\":").Append(WebworksJson.Quote(def.DefId)).Append(",\"quantity\":1}");
			}
			body.Append(']');

			if (first)
			{
				// Nothing promotional is configured; completing with an empty result is the
				// truthful answer and keeps callers from waiting forever.
				var empty = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));
				pResultHandle = empty;
				MainThreadDispatcher.Enqueue(() => CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
				{
					m_handle = empty,
					m_result = EResult.k_EResultOK,
				}));
				return true;
			}

			return PostThenRefresh("/Inventory/GrantSelf", "{\"items\":" + body + "}", out pResultHandle, "GrantPromoItems");
		}

		public static bool AddPromoItem(out SteamInventoryResult_t pResultHandle, SteamItemDef_t itemDef)
		{
			return AddPromoItems(out pResultHandle, new[] { itemDef }, 1);
		}

		public static bool AddPromoItems(out SteamInventoryResult_t pResultHandle, SteamItemDef_t[] pArrayItemDefs, uint unArrayLength)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;
			var items = ItemsJson(pArrayItemDefs, null, unArrayLength);
			if (items == null) return false;
			return PostThenRefresh("/Inventory/GrantSelf", "{\"items\":" + items + "}", out pResultHandle, "AddPromoItems");
		}

		public static bool ExchangeItems(out SteamInventoryResult_t pResultHandle, SteamItemDef_t[] pArrayGenerate, uint[] punArrayGenerateQuantity, uint unArrayGenerateLength, SteamItemInstanceID_t[] pArrayDestroy, uint[] punArrayDestroyQuantity, uint unArrayDestroyLength)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;

			var generate = ItemsJson(pArrayGenerate, punArrayGenerateQuantity, unArrayGenerateLength);
			if (generate == null) return false;

			var destroy = new StringBuilder("[");
			if (pArrayDestroy != null)
				for (uint i = 0; i < unArrayDestroyLength && i < pArrayDestroy.Length; i++)
				{
					var def = DefinitionOf(pArrayDestroy[i].m_SteamItemInstanceID);
					if (def == null) return false;
					uint quantity = punArrayDestroyQuantity != null && i < punArrayDestroyQuantity.Length ? punArrayDestroyQuantity[i] : 1;
					if (i > 0) destroy.Append(',');
					destroy.Append("{\"itemDefId\":").Append(WebworksJson.Quote(def.DefId))
						.Append(",\"quantity\":").Append(Math.Max(1, quantity).ToString(CultureInfo.InvariantCulture)).Append('}');
				}
			destroy.Append(']');

			return PostThenRefresh("/Inventory/Exchange", "{\"generate\":" + generate + ",\"destroy\":" + destroy + "}", out pResultHandle, "ExchangeItems");
		}

		public static bool TransferItemQuantity(out SteamInventoryResult_t pResultHandle, SteamItemInstanceID_t itemIdSource, uint unQuantity, SteamItemInstanceID_t itemIdDest)
		{
			pResultHandle = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));

			string sourceDef, destDef;
			if (!_instanceDef.TryGetValue(itemIdSource.m_SteamItemInstanceID, out sourceDef)
				|| !_instanceDef.TryGetValue(itemIdDest.m_SteamItemInstanceID, out destDef))
				return false;

			// Webworks stores quantities per definition, so moving quantity between two
			// instances of the same definition is already reflected by ownership.
			if (sourceDef != destDef) return false;

			var result = pResultHandle;
			MainThreadDispatcher.Enqueue(() => CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
			{
				m_handle = result,
				m_result = unQuantity > 0 ? EResult.k_EResultOK : EResult.k_EResultInvalidParam,
			}));
			return unQuantity > 0;
		}

		public static void SendItemDropHeartbeat()
		{
			// Playtime-based item drops are evaluated server-side per request; there is no
			// background drop scheduler to ping.
		}

		public static bool TriggerItemDrop(out SteamInventoryResult_t pResultHandle, SteamItemDef_t dropListDefinition)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;
			if (!_definitionsLoaded) LoadItemDefinitions();

			var listName = dropListDefinition.m_SteamItemDef.ToString(CultureInfo.InvariantCulture);
			string dropId = null;
			foreach (var def in _defsById.Values)
			{
				if (string.IsNullOrEmpty(def.DropList) || !def.ClientGrantable) continue;
				if (def.DropList == listName || def.DropList == dropListDefinition.m_SteamItemDef.ToString()) { dropId = def.DefId; break; }
			}

			if (dropId == null)
			{
				var empty = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));
				pResultHandle = empty;
				MainThreadDispatcher.Enqueue(() => CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
				{
					m_handle = empty,
					m_result = EResult.k_EResultOK,
				}));
				return true;
			}

			var body = "{\"items\":[{\"itemDefId\":" + WebworksJson.Quote(dropId) + ",\"quantity\":1}]}";
			return PostThenRefresh("/Inventory/GrantSelf", body, out pResultHandle, "TriggerItemDrop");
		}

		public static bool TradeItems(out SteamInventoryResult_t pResultHandle, CSteamID steamIDTradePartner, SteamItemInstanceID_t[] pArrayGive, uint[] pArrayGiveQuantity, uint nArrayGiveLength, SteamItemInstanceID_t[] pArrayGet, uint[] pArrayGetQuantity, uint nArrayGetLength)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;

			string giveId = InstancesJson(pArrayGive, pArrayGiveQuantity, nArrayGiveLength);
			string getId = InstancesJson(pArrayGet, pArrayGetQuantity, nArrayGetLength);
			if (giveId == null || getId == null) return false;

			var body = "{\"partner\":\"" + steamIDTradePartner.m_SteamID.ToString(CultureInfo.InvariantCulture)
				+ "\",\"give\":" + giveId + ",\"get\":" + getId + "}";
			return PostThenRefresh("/Inventory/Trade", body, out pResultHandle, "TradeItems");
		}

		static string InstancesJson(SteamItemInstanceID_t[] instances, uint[] quantities, uint length)
		{
			var body = new StringBuilder("[");
			if (instances != null)
				for (uint i = 0; i < length && i < instances.Length; i++)
				{
					var def = DefinitionOf(instances[i].m_SteamItemInstanceID);
					if (def == null) return null;
					uint quantity = quantities != null && i < quantities.Length ? quantities[i] : 1;
					if (i > 0) body.Append(',');
					body.Append("{\"itemDefId\":").Append(WebworksJson.Quote(def.DefId))
						.Append(",\"quantity\":").Append(Math.Max(1, quantity).ToString(CultureInfo.InvariantCulture)).Append('}');
				}
			body.Append(']');
			return body.ToString();
		}

		public static SteamAPICall_t RequestEligiblePromoItemDefinitionsIDs(CSteamID steamID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Inventory/Definitions"));
				var entries = payload != null ? WebworksJson.Arr(payload, "definitions") : null;
				int count = 0;
				if (entries != null)
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry != null && WebworksJson.Bool(entry, "promo")) count++;
					}

				ResultBus.Complete(call, new SteamInventoryEligiblePromoItemDefIDs_t
				{
					m_result = payload != null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_steamID = steamID,
					m_numEligiblePromoItemDefs = count,
					m_bCachedData = false,
				});
				return "";
			}, "RequestEligiblePromoItemDefinitionsIDs");
			return call;
		}

		public static bool GetEligiblePromoItemDefinitionIDs(CSteamID steamID, SteamItemDef_t[] pItemDefIDs, ref uint punItemDefIDsArraySize)
		{
			if (!_definitionsLoaded) LoadItemDefinitions();
			if (pItemDefIDs == null) { punItemDefIDsArraySize = (uint)PromoDefinitions().Count; return true; }

			var promos = PromoDefinitions();
			uint count = Math.Min(punItemDefIDsArraySize, (uint)Math.Min(pItemDefIDs.Length, promos.Count));
			for (uint i = 0; i < count; i++) pItemDefIDs[i] = new SteamItemDef_t(promos[(int)i].Id);
			punItemDefIDsArraySize = count;
			return count > 0;
		}

		static List<Definition> PromoDefinitions()
		{
			var list = new List<Definition>();
			foreach (var def in _defsById.Values) if (def.Promo) list.Add(def);
			return list;
		}

		public static SteamAPICall_t StartPurchase(SteamItemDef_t[] pArrayItemDefs, uint[] punArrayQuantity, uint unArrayLength)
		{
			// Webworks has no store or wallet, so every purchase fails cleanly.
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new SteamInventoryStartPurchaseResult_t
			{
				m_result = EResult.k_EResultInvalidState,
			}));
			return call;
		}

		public static SteamAPICall_t RequestPrices()
		{
			var call = ResultBus.NewCall();
			_pricesRequested = true;
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new SteamInventoryRequestPricesResult_t
			{
				m_result = EResult.k_EResultInvalidState,
			}));
			return call;
		}

		public static uint GetNumItemsWithPrices()
		{
			// No prices exist, so there is nothing to enumerate.
			return 0;
		}

		public static bool GetItemsWithPrices(SteamItemDef_t[] pArrayItemDefs, ulong[] pCurrentPrices, ulong[] pBasePrices, uint unArrayLength)
		{
			return false;
		}

		public static bool GetItemPrice(SteamItemDef_t iDefinition, out ulong pCurrentPrice, out ulong pBasePrice)
		{
			pCurrentPrice = 0;
			pBasePrice = 0;
			return false;
		}

		public static SteamInventoryUpdateHandle_t StartUpdateProperties()
		{
			var handle = _nextPropertyHandle++;
			_stagedProperties[handle] = new Dictionary<string, string>();
			return new SteamInventoryUpdateHandle_t(handle);
		}

		public static bool RemoveProperty(SteamInventoryUpdateHandle_t handle, SteamItemInstanceID_t nItemID, string pchPropertyName)
		{
			Dictionary<string, string> staged;
			if (!_stagedProperties.TryGetValue(handle.m_SteamInventoryUpdateHandle, out staged) || string.IsNullOrEmpty(pchPropertyName)) return false;
			staged[nItemID.m_SteamItemInstanceID + "|" + pchPropertyName] = null;
			return true;
		}

		public static bool SetProperty(SteamInventoryUpdateHandle_t handle, SteamItemInstanceID_t nItemID, string pchPropertyName, string pchPropertyValue)
		{
			Dictionary<string, string> staged;
			if (!_stagedProperties.TryGetValue(handle.m_SteamInventoryUpdateHandle, out staged) || string.IsNullOrEmpty(pchPropertyName)) return false;
			staged[nItemID.m_SteamItemInstanceID + "|" + pchPropertyName] = pchPropertyValue ?? "";
			return true;
		}

		public static bool SubmitUpdateProperties(SteamInventoryUpdateHandle_t handle, out SteamInventoryResult_t pResultHandle)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;

			Dictionary<string, string> staged;
			if (!_stagedProperties.TryGetValue(handle.m_SteamInventoryUpdateHandle, out staged)) return false;
			_stagedProperties.Remove(handle.m_SteamInventoryUpdateHandle);

			var byDefinition = new Dictionary<string, Dictionary<string, string>>();
			foreach (var kv in staged)
			{
				var parts = kv.Key.Split('|');
				ulong instance;
				if (parts.Length != 2 || !ulong.TryParse(parts[0], out instance)) continue;
				var def = DefinitionOf(instance);
				if (def == null) continue;
				if (!byDefinition.ContainsKey(def.DefId)) byDefinition[def.DefId] = new Dictionary<string, string>();
				byDefinition[def.DefId][parts[1]] = kv.Value;
			}

			if (byDefinition.Count == 0) return false;

			pResultHandle = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));
			foreach (var kv in byDefinition)
			{
				var body = new StringBuilder("{\"itemDefId\":").Append(WebworksJson.Quote(kv.Key)).Append(",\"properties\":{");
				var first = true;
				foreach (var property in kv.Value)
				{
					if (!first) body.Append(',');
					first = false;
					body.Append(WebworksJson.Quote(property.Key)).Append(':')
						.Append(property.Value == null ? "null" : WebworksJson.Quote(property.Value));
				}
				body.Append("}}");
				ApiClient.Fire(() => ApiClient.PostAsync("/Inventory/Properties", body.ToString()), "SubmitUpdateProperties");
			}

			var result = pResultHandle;
			RefreshItems((items, status) => CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t { m_handle = result, m_result = status }));
			return true;
		}

		public static bool InspectItem(out SteamInventoryResult_t pResultHandle, string pchItemToken)
		{
			pResultHandle = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));
			if (string.IsNullOrEmpty(pchItemToken)) return false;

			var body = "{\"token\":" + WebworksJson.Quote(pchItemToken) + "}";
			var handle = pResultHandle;
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Inventory/Inspect", body));
				var item = payload != null ? WebworksJson.Map(payload, "item") : null;
				var items = new List<SteamItemDetails_t>();
				if (item != null)
				{
					var defId = WebworksJson.Str(item, "itemDefId") ?? "";
					var def = IdFor(defId);
					var instance = (ulong)def * 100000UL + (ulong)(_nextInstance++);
					_instanceDef[instance] = defId;
					items.Add(new SteamItemDetails_t
					{
						m_itemId = new SteamItemInstanceID_t(instance),
						m_iDefinition = new SteamItemDef_t(def),
						m_unQuantity = 1,
					});
				}
				CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
				{
					m_handle = handle,
					m_result = item != null ? EResult.k_EResultOK : EResult.k_EResultFail,
				});
				return "";
			}, "InspectItem");
			return true;
		}
	}
}
