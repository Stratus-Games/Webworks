using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Webworks.Internal
{
	internal static class TaskUtil
	{
		public static async void Forget(this Task task, string what)
		{
			try { await task; }
			catch (Exception ex) { Debug.LogWarning("[Webworks] " + what + " failed: " + ex.GetBaseException().Message); }
		}
	}
}
