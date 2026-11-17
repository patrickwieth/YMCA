using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OpenRA.Mods.CA
{
	// Only explicitly registered wizard exports belong to this process. Never
	// scan the map folder or infer ownership from Author/title/seed alone.
	sealed class TemporaryGeneratedMapFiles
	{
		const string Prefix = "ymca-session-";
		readonly Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase);

		public static string NewFilename() => Prefix + Guid.NewGuid().ToString("N") + ".oramap";

		public void Register(string path)
		{
			path = Path.GetFullPath(path);
			var name = Path.GetFileNameWithoutExtension(path);
			if (!string.Equals(Path.GetExtension(path), ".oramap", StringComparison.OrdinalIgnoreCase) ||
				!name.StartsWith(Prefix, StringComparison.Ordinal) || !Guid.TryParseExact(name.Substring(Prefix.Length), "N", out _))
				throw new ArgumentException("Only uniquely named temporary generator exports may be registered.", nameof(path));
			// Do not replace the original fingerprint if the editor later saves here.
			if (files.ContainsKey(path)) return;
			using var stream = File.OpenRead(path);
			using var hash = SHA256.Create();
			files.Add(path, hash.ComputeHash(stream));
		}

		public void Cleanup(Action<string> report)
		{
			foreach (var pair in files.ToArray())
			{
				try
				{
					if (File.Exists(pair.Key))
					{
						byte[] current;
						using (var stream = File.OpenRead(pair.Key))
						using (var hash = SHA256.Create()) current = hash.ComputeHash(stream);
						if (current.SequenceEqual(pair.Value)) File.Delete(pair.Key);
						else report("Preserving edited temporary map: " + pair.Key);
					}
					files.Remove(pair.Key);
				}
				catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
				{
					// A lock or permission failure must never prevent game shutdown.
					report("Could not remove temporary generated map: " + pair.Key + ": " + e.Message);
				}
			}
		}
	}

	static class TemporaryGeneratedMaps
	{
		static readonly TemporaryGeneratedMapFiles Files = new();
		static bool hooked;

		public static void Register(string path)
		{
			Files.Register(path);
			if (hooked) return;
			hooked = true;
			// OnQuit runs after WorldRenderer and ModData/MapCache have disposed
			// their map packages. No active match or lobby loses its backing file.
			Game.OnQuit += () => Files.Cleanup(message => Log.Write("debug", message));
		}
	}
}
