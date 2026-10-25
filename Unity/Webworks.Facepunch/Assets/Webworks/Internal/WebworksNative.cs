#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Webworks.Internal
{
	internal static class WebworksNative
	{
		[DllImport("__Internal")] public static extern string Webworks_HttpSync(string method, string url, string body, string token);
		[DllImport("__Internal")] public static extern void Webworks_WS_Connect(int handle, string url);
		[DllImport("__Internal")] public static extern void Webworks_WS_Send(int handle, string message);
		[DllImport("__Internal")] public static extern string Webworks_WS_Poll(int handle);
		[DllImport("__Internal")] public static extern int Webworks_WS_State(int handle);
		[DllImport("__Internal")] public static extern string Webworks_WS_CloseInfo(int handle);
		[DllImport("__Internal")] public static extern void Webworks_WS_Close(int handle);
		[DllImport("__Internal")] public static extern int Webworks_PageIsSecure();
		[DllImport("__Internal")] public static extern string Webworks_QueryParam(string name);
		[DllImport("__Internal")] public static extern void Webworks_Clipboard_Init();
		[DllImport("__Internal")] public static extern void Webworks_Clipboard_Copy(string text);
		[DllImport("__Internal")] public static extern string Webworks_Clipboard_TakePaste();
		[DllImport("__Internal")] public static extern void Webworks_PickImage();
		[DllImport("__Internal")] public static extern string Webworks_TakePickedImage();
	}
}
#endif
