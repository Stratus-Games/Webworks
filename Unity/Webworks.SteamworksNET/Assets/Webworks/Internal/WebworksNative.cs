#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Webworks.SNetInternal
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
		[DllImport("__Internal")] public static extern void Webworks_Mic_Start(int consumer, int rate);
		[DllImport("__Internal")] public static extern void Webworks_Mic_Stop(int consumer);
		[DllImport("__Internal")] public static extern int Webworks_Mic_State();
		[DllImport("__Internal")] public static extern int Webworks_Mic_Available(int consumer);
		[DllImport("__Internal")] public static extern int Webworks_Mic_Read(int consumer, byte[] buffer, int maxSamples);
		[DllImport("__Internal")] public static extern void Webworks_HTML_Open(int handle, string url);
		[DllImport("__Internal")] public static extern void Webworks_HTML_Navigate(int handle, string url);
		[DllImport("__Internal")] public static extern void Webworks_HTML_Resize(int handle, int width, int height);
		[DllImport("__Internal")] public static extern void Webworks_HTML_Execute(int handle, string script);
		[DllImport("__Internal")] public static extern void Webworks_HTML_Visible(int handle, int visible);
		[DllImport("__Internal")] public static extern void Webworks_HTML_Close(int handle);
		[DllImport("__Internal")] public static extern int Webworks_Saves_Init();
		[DllImport("__Internal")] public static extern int Webworks_Saves_Ready();
		[DllImport("__Internal")] public static extern string Webworks_Saves_Take();
		[DllImport("__Internal")] public static extern int Webworks_Saves_Write(string name, string dataBase64);
		[DllImport("__Internal")] public static extern int Webworks_Saves_Delete(string name);
		[DllImport("__Internal")] public static extern int Webworks_Saves_Clear();
		[DllImport("__Internal")] public static extern void Webworks_PickImage();
		[DllImport("__Internal")] public static extern string Webworks_TakePickedImage();
	}
}
#endif
