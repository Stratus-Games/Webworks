using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Webworks.SNetInternal
{
	internal static class TunnelClient
	{
		static RealtimeClient _socket;
		static Task _connecting;
		static int _nextStream = 1;
		static readonly object _lock = new object();

		public static bool Ready { get { return _socket != null; } }

		public static event Action<int, byte[]> OnData;
		public static event Action<int, string> OnClosed;
		public static event Action<int, string, bool> OnOpened;
		public static event Action<int, int> OnListening;
		public static event Action<int, string, ushort> OnAccepted;

		public static int NextStream() { return Interlocked.Increment(ref _nextStream); }

		public static Task EnsureConnectedAsync()
		{
			lock (_lock)
			{
				if (_socket != null) return Task.FromResult(true);
				if (_connecting != null) return _connecting;
				_connecting = ConnectAsync();
				return _connecting;
			}
		}

		static async Task ConnectAsync()
		{
			try
			{
				var socket = new RealtimeClient(7);
				socket.OnMessage += Handle;
				socket.OnDisconnected += () =>
				{
					lock (_lock) { _socket = null; _connecting = null; }
				};
				await socket.ConnectAsync(WebworksConfig.TunnelUrl + "?session=" + SessionState.SessionToken);
				lock (_lock) { _socket = socket; }
			}
			catch (Exception ex)
			{
				lock (_lock) { _socket = null; _connecting = null; }
				Debug.LogWarning("[Webworks] tunnel connection failed: " + ex.Message);
			}
		}

		static void Handle(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			var op = WebworksJson.Str(d, "op");
			var id = WebworksJson.Int(d, "id");

			switch (op)
			{
				case "data":
				{
					var b64 = WebworksJson.Str(d, "data");
					byte[] bytes;
					try { bytes = Convert.FromBase64String(b64 ?? ""); }
					catch (FormatException) { return; }
					if (OnData != null) OnData(id, bytes);
					return;
				}
				case "open":
					if (OnOpened != null) OnOpened(id, WebworksJson.Str(d, "protocol") ?? "tcp", true);
					return;
				case "listening":
					if (OnListening != null) OnListening(id, WebworksJson.Int(d, "port"));
					return;
				case "accept":
					if (OnAccepted != null) OnAccepted(id, WebworksJson.Str(d, "host") ?? "", (ushort)WebworksJson.Int(d, "port"));
					return;
				case "close":
					if (OnClosed != null) OnClosed(id, WebworksJson.Str(d, "error"));
					return;
			}
		}

		static void Send(string json)
		{
			if (!Ready)
			{
				EnsureConnectedAsync().ContinueWith((task) =>
				{
					if (task.IsFaulted || _socket == null) return;
					_socket.SendAsync(json).Forget("tunnel send");
				});
				return;
			}
			_socket.SendAsync(json).Forget("tunnel send");
		}

		public static void Open(int stream, bool udp, string host, int port)
		{
			Send("{\"op\":\"open\",\"id\":" + stream + ",\"protocol\":\"" + (udp ? "udp" : "tcp") + "\",\"host\":" + WebworksJson.Quote(host) + ",\"port\":" + port + "}");
		}

		public static void Listen(int stream, int port)
		{
			Send("{\"op\":\"listen\",\"id\":" + stream + ",\"port\":" + port + "}");
		}

		public static void Send(int stream, byte[] data, int offset, int length)
		{
			if (data == null || length <= 0) return;
			Send("{\"op\":\"data\",\"id\":" + stream + ",\"data\":\"" + Convert.ToBase64String(data, offset, length) + "\"}");
		}

		public static void Close(int stream)
		{
			Send("{\"op\":\"close\",\"id\":" + stream + "}");
		}

		public static void Reset()
		{
			if (_socket != null) _socket.Close();
			lock (_lock) { _socket = null; _connecting = null; }
			_nextStream = 1;
		}
	}
}
