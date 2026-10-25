#pragma warning disable CS0067, CS0108
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;

namespace Steamworks
{
	public static class Dispatch
	{
		public static Action<CallbackType, string, bool> OnDebugCallback;
		public static Action<Exception> OnException;
	}

	public static class Utility
	{
		public static string FormatPrice(string currency, double price)
		{
			return (price / 100.0).ToString("0.00", CultureInfo.InvariantCulture) + " " + currency;
		}

		public static IPAddress Int32ToIp(uint ipaddress)
		{
			return new IPAddress(new[] { (byte)(ipaddress >> 24), (byte)(ipaddress >> 16), (byte)(ipaddress >> 8), (byte)ipaddress });
		}

		public static uint IpToInt32(IPAddress ipAddress)
		{
			var b = ipAddress.GetAddressBytes();
			return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
		}

		public static string ReadNullTerminatedUTF8String(IntPtr ptr, byte[] buffer = null)
		{
			if (ptr == IntPtr.Zero) return null;
			int length = 0;
			while (Marshal.ReadByte(ptr, length) != 0) length++;
			var bytes = new byte[length];
			Marshal.Copy(ptr, bytes, 0, length);
			return Encoding.UTF8.GetString(bytes);
		}
	}
}

namespace Steamworks.Data
{
	public struct OutgoingPacket
	{
		public uint Address;
		public ushort Port;
		public byte[] Data;
		public int Size;
	}

	public struct RemotePlaySession
	{
		public uint Id;
		public SteamId SteamId;
		public string ClientName;
		public SteamDeviceFormFactor FormFactor;
		public bool IsValid { get { return Id != 0; } }
	}

	public struct Screenshot
	{
		public bool SetLocation(string location) { return SteamScreenshots.SetLocation(this, location); }
		public bool TagPublishedFile(PublishedFileId file) { return SteamScreenshots.TagPublishedFile(this, file); }
		public bool TagUser(SteamId user) { return SteamScreenshots.TagUser(this, user); }
		internal ulong Handle;
	}

	public struct InventoryDefId
	{
		public int Value;
		public InventoryDefId(int value) { Value = value; }
		public static implicit operator InventoryDefId(int value) { return new InventoryDefId { Value = value }; }
		public static implicit operator int(InventoryDefId id) { return id.Value; }
	}

	public struct InventoryItemId
	{
		public ulong Value;
		public InventoryItemId(ulong value) { Value = value; }
		public static implicit operator InventoryItemId(ulong value) { return new InventoryItemId { Value = value }; }
		public static implicit operator ulong(InventoryItemId id) { return id.Value; }
	}

	public struct InventoryPurchaseResult
	{
		public Result Result;
		public ulong OrderID;
		public ulong TransID;
	}

	public struct ServerInfo
	{
		public string Name;
		public int Ping;
		public string GameDir;
		public string Map;
		public string Description;
		public uint AppId;
		public int Players;
		public int MaxPlayers;
		public int BotPlayers;
		public bool Passworded;
		public bool Secure;
		public uint LastTimePlayed;
		public int Version;
		public string TagString;
		public string[] Tags { get { return string.IsNullOrEmpty(TagString) ? new string[0] : TagString.Split(','); } }
		public SteamId SteamId;
		public uint AddressRaw;
		public IPAddress Address { get { return new IPAddress(new[] { (byte)(AddressRaw >> 24), (byte)(AddressRaw >> 16), (byte)(AddressRaw >> 8), (byte)AddressRaw }); } }
		public int ConnectionPort;
		public int QueryPort;

		public void AddToHistory() { ServerList.LocalServerLists.History.Add(SteamId.Value); }
		public void RemoveFromHistory() { ServerList.LocalServerLists.History.Remove(SteamId.Value); }
		public void AddToFavourites() { ServerList.LocalServerLists.Favourites.Add(SteamId.Value); }
		public void RemoveFromFavourites() { ServerList.LocalServerLists.Favourites.Remove(SteamId.Value); }
		public Task<Dictionary<string, string>> QueryRulesAsync() { return Task.FromResult(new Dictionary<string, string>()); }
	}
}
