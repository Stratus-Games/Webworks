using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Webworks.SNetInternal
{
	internal static class WorkshopContent
	{
		static readonly HashSet<ulong> _subscribed = new HashSet<ulong>();
		static readonly HashSet<ulong> _downloading = new HashSet<ulong>();

		static string _root;
		static string Root
		{
			get { return _root ?? (_root = Path.Combine(Application.persistentDataPath, "webworks_workshop")); }
		}

		public static string DirectoryFor(ulong fileId)
		{
			return Path.Combine(Root, fileId.ToString());
		}

		public static bool IsInstalled(ulong fileId)
		{
			try { return File.Exists(Path.Combine(DirectoryFor(fileId), ".webworks-installed")); }
			catch { return false; }
		}

		public static bool IsDownloading(ulong fileId) { return _downloading.Contains(fileId); }

		public static bool IsSubscribed(ulong fileId) { return _subscribed.Contains(fileId); }
		public static void SetSubscribed(ulong fileId, bool subscribed)
		{
			if (subscribed) _subscribed.Add(fileId); else _subscribed.Remove(fileId);
		}

		public static bool WriteContent(ulong fileId, Dictionary<string, object> content)
		{
			var dir = DirectoryFor(fileId);
			try
			{
				if (Directory.Exists(dir)) Directory.Delete(dir, true);
				Directory.CreateDirectory(dir);

				if (content != null)
				{
					foreach (var kv in content)
					{
						var relPath = kv.Key;
						var b64 = kv.Value as string;
						if (string.IsNullOrEmpty(relPath) || b64 == null) continue;
						if (relPath.IndexOf("..") >= 0 || Path.IsPathRooted(relPath)) continue;

						var full = Path.Combine(dir, relPath.Replace('/', Path.DirectorySeparatorChar));
						var parent = Path.GetDirectoryName(full);
						if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
						File.WriteAllBytes(full, Convert.FromBase64String(b64));
					}
				}

				File.WriteAllText(Path.Combine(dir, ".webworks-installed"), DateTime.UtcNow.ToString("o"));
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] failed to write workshop item " + fileId + " to disk: " + ex.Message);
				return false;
			}
		}

		public static void BeginDownload(ulong fileId) { _downloading.Add(fileId); }
		public static void EndDownload(ulong fileId) { _downloading.Remove(fileId); }

		public static Dictionary<string, byte[]> ReadFolder(string folderName)
		{
			var result = new Dictionary<string, byte[]>();
			if (string.IsNullOrEmpty(folderName)) return result;
#if UNITY_WEBGL && !UNITY_EDITOR
			Debug.LogWarning("[Webworks] Editor.WithContent(folder) can't read an arbitrary folder in a WebGL build; publish with in-memory content instead.");
			return result;
#else
			try
			{
				if (!Directory.Exists(folderName)) return result;
				foreach (var file in Directory.GetFiles(folderName, "*", SearchOption.AllDirectories))
				{
					var rel = file.Substring(folderName.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
					result[rel] = File.ReadAllBytes(file);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] could not read Workshop content folder '" + folderName + "': " + ex.Message);
			}
			return result;
#endif
		}
	}
}
