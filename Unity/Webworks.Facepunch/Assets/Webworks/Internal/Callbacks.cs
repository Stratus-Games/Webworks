using System;
using UnityEngine;

namespace Webworks.Internal
{
	internal static class Callbacks
	{
		static void Log(Exception e) { Debug.LogException(e); }

		public static void Raise<T>(Action<T> ev, T a)
		{
			if (ev == null) return;
			MainThreadDispatcher.Enqueue(() => RaiseNow(ev, a));
		}

		public static void Raise<T1, T2>(Action<T1, T2> ev, T1 a, T2 b)
		{
			if (ev == null) return;
			MainThreadDispatcher.Enqueue(() => RaiseNow(ev, a, b));
		}

		public static void Raise<T1, T2, T3>(Action<T1, T2, T3> ev, T1 a, T2 b, T3 c)
		{
			if (ev == null) return;
			MainThreadDispatcher.Enqueue(() => RaiseNow(ev, a, b, c));
		}

		public static void Raise<T1, T2, T3, T4>(Action<T1, T2, T3, T4> ev, T1 a, T2 b, T3 c, T4 d)
		{
			if (ev == null) return;
			MainThreadDispatcher.Enqueue(() => RaiseNow(ev, a, b, c, d));
		}

		public static void RaiseNow<T>(Action<T> ev, T a)
		{
			if (ev == null) return;
			foreach (var d in ev.GetInvocationList())
			{
				try { ((Action<T>)d)(a); } catch (Exception e) { Log(e); }
			}
		}

		public static void RaiseNow<T1, T2>(Action<T1, T2> ev, T1 a, T2 b)
		{
			if (ev == null) return;
			foreach (var d in ev.GetInvocationList())
			{
				try { ((Action<T1, T2>)d)(a, b); } catch (Exception e) { Log(e); }
			}
		}

		public static void RaiseNow<T1, T2, T3>(Action<T1, T2, T3> ev, T1 a, T2 b, T3 c)
		{
			if (ev == null) return;
			foreach (var d in ev.GetInvocationList())
			{
				try { ((Action<T1, T2, T3>)d)(a, b, c); } catch (Exception e) { Log(e); }
			}
		}

		public static void RaiseNow<T1, T2, T3, T4>(Action<T1, T2, T3, T4> ev, T1 a, T2 b, T3 c, T4 d4)
		{
			if (ev == null) return;
			foreach (var d in ev.GetInvocationList())
			{
				try { ((Action<T1, T2, T3, T4>)d)(a, b, c, d4); } catch (Exception e) { Log(e); }
			}
		}
	}
}
