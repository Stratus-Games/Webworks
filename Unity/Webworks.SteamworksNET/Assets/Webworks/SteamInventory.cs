using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public struct SteamInventoryResultReady_t
	{
		public SteamInventoryResult_t m_handle;
		public EResult m_result;
	}

	public struct SteamInventoryFullUpdate_t
	{
		public SteamInventoryResult_t m_handle;
	}

	public struct SteamInventoryDefinitionUpdate_t { }

	public static partial class SteamInventory
	{
		class Definition
		{
			public int Id;
			public string DefId;
			public string Name;
			public string Description;
			public string Type;
			public string IconUrl;
			public bool Tradable;
			public bool Marketable;
			public bool ClientGrantable;
			public bool Promo;
			public string DropList = "";
		}

		static readonly Dictionary<string, int> _idsByDef = new Dictionary<string, int>();
		static readonly Dictionary<int, Definition> _defsById = new Dictionary<int, Definition>();
		static readonly Dictionary<int, List<SteamItemDetails_t>> _results = new Dictionary<int, List<SteamItemDetails_t>>();
		static int _nextResult = 1;
		static int _nextInstance = 1;
		static readonly Dictionary<ulong, string> _instanceDef = new Dictionary<ulong, string>();
		static bool _definitionsLoaded;

		static int IdFor(string defId)
		{
			int id;
			if (_idsByDef.TryGetValue(defId, out id)) return id;
			id = defId.GetHashCode() & 0x7fffffff;
			if (id == 0) id = _idsByDef.Count + 1;
			while (_defsById.ContainsKey(id)) id++;
			_idsByDef[defId] = id;
			return id;
		}

		public static bool LoadItemDefinitions()
		{
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Inventory/Definitions"));
				var entries = payload != null ? WebworksJson.Arr(payload, "definitions") : null;
				_definitionsLoaded = true;

				_defsById.Clear();
				_idsByDef.Clear();
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry == null) continue;
						var defId = WebworksJson.Str(entry, "itemDefId") ?? "";
						var definition = new Definition
						{
							Id = IdFor(defId),
							DefId = defId,
							Name = WebworksJson.Str(entry, "name") ?? defId,
							Description = WebworksJson.Str(entry, "description") ?? "",
							Type = WebworksJson.Str(entry, "type") ?? "item",
							IconUrl = WebworksJson.Str(entry, "iconUrl") ?? "",
						Tradable = WebworksJson.Bool(entry, "tradable"),
						Marketable = WebworksJson.Bool(entry, "marketable"),
						ClientGrantable = WebworksJson.Bool(entry, "clientGrantable"),
						Promo = WebworksJson.Bool(entry, "promo"),
						DropList = WebworksJson.Str(entry, "dropList") ?? "",
					};
						_defsById[definition.Id] = definition;
					}
				}
				CallbackBus<SteamInventoryDefinitionUpdate_t>.Fire(new SteamInventoryDefinitionUpdate_t());
				return "";
			}, "LoadItemDefinitions");
			return true;
		}

		public static bool GetItemDefinitionIDs(SteamItemDef_t[] pItemDefIDs, ref uint punItemDefIDsArraySize)
		{
			if (!_definitionsLoaded) LoadItemDefinitions();
			if (pItemDefIDs == null) { punItemDefIDsArraySize = (uint)_defsById.Count; return true; }

			uint index = 0;
			foreach (var def in _defsById.Values)
			{
				if (index >= punItemDefIDsArraySize || index >= pItemDefIDs.Length) break;
				pItemDefIDs[index++] = new SteamItemDef_t(def.Id);
			}
			punItemDefIDsArraySize = index;
			return index > 0;
		}

		public static bool GetItemDefinitionProperty(SteamItemDef_t iDefinition, string pchPropertyName, out string pchValueBuffer, ref uint punValueBufferSizeOut)
		{
			pchValueBuffer = "";
			Definition def;
			if (!_defsById.TryGetValue(iDefinition.m_SteamItemDef, out def)) return false;

			switch ((pchPropertyName ?? "").ToLowerInvariant())
			{
				case "name": pchValueBuffer = def.Name; break;
				case "description": pchValueBuffer = def.Description; break;
				case "type": pchValueBuffer = def.Type; break;
				case "icon_url": case "icon_url_large": pchValueBuffer = def.IconUrl; break;
				case "tradable": pchValueBuffer = def.Tradable ? "1" : "0"; break;
				case "marketable": pchValueBuffer = def.Marketable ? "1" : "0"; break;
				case "client_grantable": pchValueBuffer = def.ClientGrantable ? "1" : "0"; break;
				default: return false;
			}
			punValueBufferSizeOut = (uint)pchValueBuffer.Length;
			return true;
		}

		static int NewResult(List<SteamItemDetails_t> items)
		{
			var handle = _nextResult++;
			_results[handle] = items ?? new List<SteamItemDetails_t>();
			return handle;
		}

		public static bool GetAllItems(out SteamInventoryResult_t pResultHandle)
		{
			if (!_definitionsLoaded) LoadItemDefinitions();

			var snapshot = new List<SteamItemDetails_t>();
			foreach (var entry in _results.Values) { }
			var cached = _lastAllItems ?? new List<SteamItemDetails_t>();
			foreach (var item in cached) snapshot.Add(item);

			pResultHandle = new SteamInventoryResult_t(NewResult(snapshot));

			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Inventory/Items"));
				var entries = payload != null ? WebworksJson.Arr(payload, "items") : null;
				var items = new List<SteamItemDetails_t>();
				_instanceDef.Clear();

				if (entries != null)
				{
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
				}

				_lastAllItems = items;
				CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
				{
					m_handle = new SteamInventoryResult_t(NewResult(items)),
					m_result = EResult.k_EResultOK,
				});
				return "";
			}, "GetAllItems");

			return true;
		}

		static List<SteamItemDetails_t> _lastAllItems;

		public static bool GetItemsByID(out SteamInventoryResult_t pResultHandle, SteamItemInstanceID_t[] pInstanceIDs, uint unCountInstanceIDs)
		{
			var items = new List<SteamItemDetails_t>();
			var all = _lastAllItems ?? new List<SteamItemDetails_t>();
			if (pInstanceIDs != null)
			{
				for (uint i = 0; i < unCountInstanceIDs && i < pInstanceIDs.Length; i++)
				{
					foreach (var item in all)
						if (item.m_itemId.m_SteamItemInstanceID == pInstanceIDs[i].m_SteamItemInstanceID) items.Add(item);
				}
			}
			pResultHandle = new SteamInventoryResult_t(NewResult(items));
			return items.Count > 0;
		}

		public static EResult GetResultStatus(SteamInventoryResult_t resultHandle)
		{
			return _results.ContainsKey(resultHandle.m_SteamInventoryResult) ? EResult.k_EResultOK : EResult.k_EResultInvalidParam;
		}

		public static bool GetResultItems(SteamInventoryResult_t resultHandle, SteamItemDetails_t[] pOutItemsArray, ref uint punOutItemsArraySize)
		{
			List<SteamItemDetails_t> items;
			if (!_results.TryGetValue(resultHandle.m_SteamInventoryResult, out items)) return false;
			if (pOutItemsArray == null) { punOutItemsArraySize = (uint)items.Count; return true; }

			uint count = Math.Min(punOutItemsArraySize, (uint)Math.Min(pOutItemsArray.Length, items.Count));
			for (uint i = 0; i < count; i++) pOutItemsArray[i] = items[(int)i];
			punOutItemsArraySize = count;
			return true;
		}

		public static bool GetResultItemProperty(SteamInventoryResult_t resultHandle, uint unItemIndex, string pchPropertyName, out string pchValueBuffer, ref uint punValueBufferSizeOut)
		{
			pchValueBuffer = "";
			List<SteamItemDetails_t> items;
			if (!_results.TryGetValue(resultHandle.m_SteamInventoryResult, out items) || unItemIndex >= items.Count) return false;
			return GetItemDefinitionProperty(items[(int)unItemIndex].m_iDefinition, pchPropertyName, out pchValueBuffer, ref punValueBufferSizeOut);
		}

		public static uint GetResultTimestamp(SteamInventoryResult_t resultHandle)
		{
			return (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
		}

		public static bool CheckResultSteamID(SteamInventoryResult_t resultHandle, CSteamID steamIDExpected) { return true; }

		public static void DestroyResult(SteamInventoryResult_t resultHandle)
		{
			_results.Remove(resultHandle.m_SteamInventoryResult);
		}

		public static bool ConsumeItem(out SteamInventoryResult_t pResultHandle, SteamItemInstanceID_t itemConsume, uint unQuantity)
		{
			pResultHandle = SteamInventoryResult_t.Invalid;

			string defId;
			if (!_instanceDef.TryGetValue(itemConsume.m_SteamItemInstanceID, out defId))
			{
				var definition = (int)(itemConsume.m_SteamItemInstanceID / 100000UL);
				Definition known;
				if (_defsById.TryGetValue(definition, out known)) defId = known.DefId;
			}

			if (string.IsNullOrEmpty(defId)) return false;

			var body = "{\"itemDefId\":" + WebworksJson.Quote(defId) + ",\"quantity\":" + Math.Max(1, unQuantity) + "}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Inventory/Consume", body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				CallbackBus<SteamInventoryResultReady_t>.Fire(new SteamInventoryResultReady_t
				{
					m_handle = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>())),
					m_result = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
				});
				return "";
			}, "ConsumeItem");

			pResultHandle = new SteamInventoryResult_t(NewResult(new List<SteamItemDetails_t>()));
			return true;
		}

		public static bool SerializeResult(SteamInventoryResult_t resultHandle, byte[] pOutBuffer, out uint punOutBufferSize)
		{
			List<SteamItemDetails_t> items;
			punOutBufferSize = 0;
			if (!_results.TryGetValue(resultHandle.m_SteamInventoryResult, out items)) return false;

			var sb = new StringBuilder("[");
			for (int i = 0; i < items.Count; i++)
			{
				if (i > 0) sb.Append(',');
				sb.Append("{\"i\":").Append(items[i].m_itemId.m_SteamItemInstanceID)
					.Append(",\"d\":").Append(items[i].m_iDefinition.m_SteamItemDef)
					.Append(",\"q\":").Append(items[i].m_unQuantity).Append('}');
			}
			sb.Append(']');

			var bytes = Encoding.UTF8.GetBytes(sb.ToString());
			punOutBufferSize = (uint)bytes.Length;
			if (pOutBuffer != null && pOutBuffer.Length >= bytes.Length) Array.Copy(bytes, pOutBuffer, bytes.Length);
			return pOutBuffer != null && pOutBuffer.Length >= bytes.Length;
		}

		public static bool DeserializeResult(out SteamInventoryResult_t pOutResultHandle, byte[] pBuffer, uint unBufferSize, bool bRESERVED_MUST_BE_FALSE = false)
		{
			pOutResultHandle = SteamInventoryResult_t.Invalid;
			if (pBuffer == null || unBufferSize == 0) return false;

			try
			{
				var text = Encoding.UTF8.GetString(pBuffer, 0, (int)Math.Min(unBufferSize, pBuffer.Length));
				var items = new List<SteamItemDetails_t>();
				int index = 0;
				while (true)
				{
					int start = text.IndexOf("{\"i\":", index, StringComparison.Ordinal);
					if (start < 0) break;
					int end = text.IndexOf('}', start);
					if (end < 0) break;
					var body = text.Substring(start + 1, end - start - 1);
					var parts = body.Split(',');
					ulong instance = 0;
					int def = 0;
					ushort quantity = 1;
					foreach (var part in parts)
					{
						var kv = part.Split(':');
						if (kv.Length != 2) continue;
						var key = kv[0].Trim('"');
						uint value;
						uint.TryParse(kv[1], out value);
						if (key == "i") instance = value;
						else if (key == "d") def = (int)value;
						else if (key == "q") quantity = (ushort)value;
					}
					items.Add(new SteamItemDetails_t
					{
						m_itemId = new SteamItemInstanceID_t(instance),
						m_iDefinition = new SteamItemDef_t(def),
						m_unQuantity = quantity,
					});
					index = end;
				}

				pOutResultHandle = new SteamInventoryResult_t(NewResult(items));
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public static partial class SteamScreenshots
	{
		static bool _hooked;
		static readonly Dictionary<uint, string> _locations = new Dictionary<uint, string>();
		static uint _nextHandle = 1;

		static void Upload(byte[] png, int width, int height, uint handle, string location)
		{
			var body = new StringBuilder("{");
			body.Append("\"data\":").Append(WebworksJson.Quote(Convert.ToBase64String(png)));
			body.Append(",\"width\":").Append(width);
			body.Append(",\"height\":").Append(height);
			body.Append(",\"location\":").Append(WebworksJson.Quote(location ?? ""));
			body.Append(",\"caption\":\"\"}");
			ApiClient.Fire(() => ApiClient.PostAsync("/Screenshots/Add", body.ToString()), "screenshot upload");
		}

		public static ScreenshotHandle WriteScreenshot(byte[] pubRGB, uint cubRGB, int nWidth, int nHeight)
		{
			if (pubRGB == null || nWidth <= 0 || nHeight <= 0) return new ScreenshotHandle(0);

			try
			{
				var texture = new Texture2D(nWidth, nHeight, TextureFormat.RGB24, false);
				texture.LoadRawTextureData(pubRGB);
				texture.Apply();
				var png = texture.EncodeToPNG();
				UnityEngine.Object.Destroy(texture);

				var handle = _nextHandle++;
				Upload(png, nWidth, nHeight, handle, null);
				return new ScreenshotHandle(handle);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] WriteScreenshot failed: " + ex.Message);
				return new ScreenshotHandle(0);
			}
		}

		public static ScreenshotHandle AddScreenshotToLibrary(string pchFilename, string pchThumbnailFilename, int nWidth, int nHeight)
		{
			try
			{
				var bytes = File.ReadAllBytes(pchFilename);
				var handle = _nextHandle++;
				Upload(bytes, nWidth, nHeight, handle, Path.GetFileName(pchFilename));
				return new ScreenshotHandle(handle);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] AddScreenshotToLibrary failed: " + ex.Message);
				return new ScreenshotHandle(0);
			}
		}

		public static void TriggerScreenshot()
		{
			try
			{
				var texture = ScreenCapture.CaptureScreenshotAsTexture();
				var png = texture.EncodeToPNG();
				var handle = _nextHandle++;
				Upload(png, texture.width, texture.height, handle, _locations.ContainsKey(handle) ? _locations[handle] : null);
				UnityEngine.Object.Destroy(texture);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] TriggerScreenshot failed: " + ex.Message);
			}
		}

		public static void HookScreenshots(bool bHook) { _hooked = bHook; }
		public static bool IsScreenshotsHooked() { return _hooked; }

		public static bool SetLocation(ScreenshotHandle hScreenshot, string pchLocation)
		{
			_locations[hScreenshot.m_ScreenshotHandle] = pchLocation ?? "";
			return true;
		}

		public static bool TagUser(ScreenshotHandle hScreenshot, CSteamID steamID) { return true; }
		public static bool TagPublishedFile(ScreenshotHandle hScreenshot, PublishedFileId_t unPublishedFileID) { return true; }
		public static ScreenshotHandle AddVRScreenshotToLibrary(EVRScreenshotType eType, string pchFilename, string pchVRFilename) { return new ScreenshotHandle(0); }
	}
}
