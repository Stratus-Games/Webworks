using System;
using System.Text;

namespace Steamworks
{
	public static class InteropHelp
	{
		public static string ByteArrayToStringUTF8(byte[] array)
		{
			if (array == null) return string.Empty;
			int count = Array.IndexOf<byte>(array, 0);
			if (count < 0) count = array.Length;
			return Encoding.UTF8.GetString(array, 0, count);
		}

		public static void StringToByteArrayUTF8(string value, byte[] array, int size)
		{
			if (array == null) return;
			Array.Clear(array, 0, array.Length);
			if (string.IsNullOrEmpty(value)) return;
			var bytes = Encoding.UTF8.GetBytes(value);
			Array.Copy(bytes, 0, array, 0, Math.Min(bytes.Length, Math.Min(size, array.Length) - 1));
		}
	}
}
