using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Webworks.SNetInternal
{
	public class MainThreadDispatcher : MonoBehaviour
	{
		static MainThreadDispatcher _instance;
		static readonly Queue<Action> _queue = new Queue<Action>();
		static readonly object _lock = new object();

		public static void Run(IEnumerator routine)
		{
			if (_instance == null) Bootstrap();
			_instance.StartCoroutine(routine);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Bootstrap()
		{
			if (_instance != null) return;
			Application.runInBackground = true;
			var go = new GameObject("[Webworks.MainThreadDispatcher]");
			UnityEngine.Object.DontDestroyOnLoad(go);
			_instance = go.AddComponent<MainThreadDispatcher>();
#if UNITY_WEBGL && !UNITY_EDITOR
			go.AddComponent<WebworksClipboard>();
#endif
		}

		public static void Enqueue(Action action)
		{
			if (action == null) return;
			lock (_lock) { _queue.Enqueue(action); }
		}

		void Update()
		{
			int processed = 0;
			while (processed < 256)
			{
				Action action;
				lock (_lock)
				{
					if (_queue.Count == 0) break;
					action = _queue.Dequeue();
				}
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
				processed++;
			}
		}
	}
}
