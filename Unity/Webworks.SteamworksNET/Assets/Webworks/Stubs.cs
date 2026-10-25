using System;
using UnityEngine;

namespace Steamworks
{
	static class StubWarn
	{
		public static void Once(ref bool flag, string who)
		{
			if (flag) return;
			flag = true;
			Debug.LogWarning("[Webworks] " + who + " isn't implemented in this shim yet -- see docs/COVERAGE.md.");
		}
	}

	
}
