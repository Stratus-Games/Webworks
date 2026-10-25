using System;
using System.Threading.Tasks;

namespace Webworks.SNetInternal
{
	public class RelayClient
	{
		static int _nextHandle = 1;
		internal static int NextHandle() { return _nextHandle++; }

		public event Action<ulong, byte[], int, bool> OnPacket;
		public event Action OnDisconnected;

		RealtimeClient _socket;
		volatile bool _ready;
		public bool IsReady { get { return _ready; } }

		public async Task<bool> ConnectAsync(string wsUrl, string ticket, int timeoutMs = 5000)
		{
			_socket = new RealtimeClient(NextHandle());
			_socket.OnMessage += HandleMessage;
			_socket.OnDisconnected += () => { _ready = false; OnDisconnected?.Invoke(); };

			await _socket.ConnectAsync(wsUrl);
			await _socket.SendAsync("{\"type\":\"hello\",\"ticket\":" + WebworksJson.Quote(ticket) + "}");

			var started = DateTime.UtcNow;
			while (!_ready && (DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)
			{
				await Task.Delay(20);
			}
			return _ready;
		}

		void HandleMessage(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;

			switch (WebworksJson.Str(d, "type"))
			{
				case "welcome":
					_ready = true;
					break;
				case "packet":
				{
					var from = WebworksJson.Ulong(d, "from");
					var b64 = WebworksJson.Str(d, "data");
					if (from == 0 || b64 == null) return;
					var channel = WebworksJson.Int(d, "channel");
					var reliable = WebworksJson.Str(d, "reliability") != "unreliable";
					OnPacket?.Invoke(from, Convert.FromBase64String(b64), channel, reliable);
					break;
				}
			}
		}

		public void Send(ulong to, byte[] data, int channel, bool reliable)
		{
			if (!_ready) return;
			var json = "{\"type\":\"send\",\"to\":" + WebworksJson.Quote(to.ToString())
				+ ",\"channel\":" + channel
				+ ",\"reliability\":" + WebworksJson.Quote(reliable ? "reliable" : "unreliable")
				+ ",\"data\":" + WebworksJson.Quote(Convert.ToBase64String(data)) + "}";
			_socket.SendAsync(json).Forget("relay send to " + to);
		}

		public void Close()
		{
			_ready = false;
			_socket?.Close();
		}
	}
}
