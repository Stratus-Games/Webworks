namespace Webworks.SNetInternal
{
	public static class SessionState
	{
		public static string SessionToken;
		public static string SteamId;
		public static string Name;
		public static string AppId;
		public static bool Initialized;

		// Persona state lives on the account (GWAS), so it survives a reload and follows
		// the player to every machine they sign in on.
		public static string PersonaState = "Online";

		public static string LinkedAccount;
	}
}
