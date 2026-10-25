using Steamworks.Data;

namespace Steamworks
{
	public static partial class SteamFriends
	{
		public static void ActivateGameOverlay(string dialog = "friends")
		{
			WebworksOverlay.Show(dialog);
		}

		public static void ActivateGameOverlayToUser(string dialog, SteamId steamId)
		{
			WebworksOverlay.ShowUser(dialog, steamId);
		}

		public static void ActivateGameOverlayToWebPage(string url)
		{
			WebworksOverlay.ShowWebPage(url);
		}

		public static void ActivateGameOverlayInviteDialog(SteamId lobbyId)
		{
			WebworksOverlay.ShowInviteDialog(lobbyId);
		}
	}

	public static partial class SteamUtils
	{
		public static void OpenControllerBindingPanel()
		{
			WebworksOverlay.ShowInfo("Controller binding",
				"Controller rebinding isn't available in a Webworks guest session -- it needs the real Steam Input overlay, which only the native Steam client can draw.");
		}

		public static void OpenBigPictureOverlay()
		{
			WebworksOverlay.ShowInfo("Big Picture",
				"Big Picture Mode isn't available in a Webworks guest session -- it's a native Steam client UI with no web equivalent.");
		}
	}
}
