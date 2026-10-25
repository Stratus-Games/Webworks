#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;

namespace Webworks.SNetInternal
{
#if UNITY_2017_1_OR_NEWER
	[DefaultExecutionOrder(-32000)]
#endif
	public class WebworksClipboard : MonoBehaviour
	{
		string _last = "";

		void Awake()
		{
			WebworksNative.Webworks_Clipboard_Init();
			_last = GUIUtility.systemCopyBuffer ?? "";
		}

		void Update()
		{
			string pasted = WebworksNative.Webworks_Clipboard_TakePaste();
			if (!string.IsNullOrEmpty(pasted))
			{
				GUIUtility.systemCopyBuffer = pasted;
				_last = pasted;
				return;
			}

			string current = GUIUtility.systemCopyBuffer ?? "";
			if (current == _last) return;
			_last = current;
			if (current.Length > 0) WebworksNative.Webworks_Clipboard_Copy(current);
		}
	}
}
#endif
