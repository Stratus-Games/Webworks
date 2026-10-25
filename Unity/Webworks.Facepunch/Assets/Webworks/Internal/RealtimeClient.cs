#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Webworks.Internal
{
	public class RealtimeClient
	{
		public event Action<string> OnMessage;
		public event Action OnConnected;
		public event Action OnDisconnected;

		readonly int _handle;
		bool _closed;

		public RealtimeClient(int handle = 0) { _handle = handle; }

		public Task ConnectAsync() { return ConnectAsync(WebworksConfig.RealtimeUrl + "?session=" + SessionState.SessionToken); }

		public Task ConnectAsync(string url)
		{
			WebworksConfig.Load();
			WebworksNative.Webworks_WS_Connect(_handle, url);
			MainThreadDispatcher.Run(Pump());
			if (OnConnected != null) OnConnected();
			return Task.FromResult(true);
		}

		public Task SendAsync(string json)
		{
			if (!_closed) WebworksNative.Webworks_WS_Send(_handle, json);
			return Task.FromResult(true);
		}

		IEnumerator Pump()
		{
			while (!_closed)
			{
				bool drained = false;
				for (int n = 0; n < 500; n++)
				{
					string m = WebworksNative.Webworks_WS_Poll(_handle);
					if (string.IsNullOrEmpty(m)) { drained = true; break; }
					if (OnMessage != null) OnMessage(m);
				}

				if (drained && WebworksNative.Webworks_WS_State(_handle) >= 3)
				{
					_closed = true;
					Debug.LogWarning("[Webworks] realtime closed: " + WebworksNative.Webworks_WS_CloseInfo(_handle));
					if (OnDisconnected != null) OnDisconnected();
					yield break;
				}
				yield return null;
			}
		}

		public void Close()
		{
			_closed = true;
			WebworksNative.Webworks_WS_Close(_handle);
		}
	}
}
#else
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Webworks.Internal
{
	public class RealtimeClient
	{
		ClientWebSocket _socket;
		CancellationTokenSource _cts;

		public RealtimeClient(int handle = 0) { }

		public event Action<string> OnMessage;
		public event Action OnConnected;
		public event Action OnDisconnected;

		public Task ConnectAsync() { return ConnectAsync(WebworksConfig.RealtimeUrl + "?session=" + SessionState.SessionToken); }

		public async Task ConnectAsync(string url)
		{
			WebworksConfig.Load();
			_socket = new ClientWebSocket();
			_cts = new CancellationTokenSource();

			var uri = new Uri(url);
			await _socket.ConnectAsync(uri, _cts.Token);

			OnConnected?.Invoke();
			ReceiveLoop().Forget("realtime loop");
		}

		class PendingSend { public byte[] Bytes; public TaskCompletionSource<bool> Done; }
		readonly Queue<PendingSend> _sendQueue = new Queue<PendingSend>();
		readonly object _sendLock = new object();
		bool _pumping;

		public Task SendAsync(string json)
		{
			if (_socket == null || _socket.State != WebSocketState.Open) return Task.FromResult(true);

			var item = new PendingSend { Bytes = Encoding.UTF8.GetBytes(json), Done = new TaskCompletionSource<bool>() };
			bool startPump;
			lock (_sendLock)
			{
				_sendQueue.Enqueue(item);
				startPump = !_pumping;
				_pumping = true;
			}
			if (startPump) PumpSends().Forget("realtime loop");
			return item.Done.Task;
		}

		async Task PumpSends()
		{
			while (true)
			{
				PendingSend item;
				lock (_sendLock)
				{
					if (_sendQueue.Count == 0) { _pumping = false; return; }
					item = _sendQueue.Dequeue();
				}

				try
				{
					if (_socket == null || _socket.State != WebSocketState.Open) { item.Done.TrySetResult(false); continue; }
					await _socket.SendAsync(new ArraySegment<byte>(item.Bytes), WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
					item.Done.TrySetResult(true);
				}
				catch (Exception ex)
				{
					item.Done.TrySetException(ex);
				}
			}
		}

		async Task ReceiveLoop()
		{
			var buffer = new byte[8192];
			var ms = new System.IO.MemoryStream();
			try
			{
				while (_socket.State == WebSocketState.Open)
				{
					var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token).ConfigureAwait(false);
					if (result.MessageType == WebSocketMessageType.Close)
					{
						Debug.LogWarning("[Webworks] realtime closed by server: " + result.CloseStatus + " " + result.CloseStatusDescription);
						OnDisconnected?.Invoke();
						break;
					}

					ms.Write(buffer, 0, result.Count);
					if (!result.EndOfMessage) continue;

					var text = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
					ms.SetLength(0);
					OnMessage?.Invoke(text);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[Webworks] realtime receive loop ended: {ex.Message}");
				OnDisconnected?.Invoke();
			}
		}

		public void Close()
		{
			_cts?.Cancel();
			_socket?.Dispose();
		}
	}
}
#endif
