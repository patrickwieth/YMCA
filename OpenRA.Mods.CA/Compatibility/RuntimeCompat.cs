using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpenRA.Mods.CA
{
	// Common .NET6/netstandard2.1 implementations; keep generated hashes and files identical.
	internal static class RuntimeCompat
	{
		public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		public static byte[] HashData(byte[] data)
		{
			using var sha = SHA256.Create();
			return sha.ComputeHash(data);
		}

		public static string ToHexString(byte[] data) => BitConverter.ToString(data).Replace("-", "");

		public static T[] EnumValues<T>() where T : struct, Enum => (T[])Enum.GetValues(typeof(T));

		public static void ReplaceFile(string source, string destination)
		{
			// Do not delete the old file before replacing it: failed writes must preserve it.
			if (File.Exists(destination))
				File.Replace(source, destination, null);
			else
				File.Move(source, destination);
		}
	}
}
