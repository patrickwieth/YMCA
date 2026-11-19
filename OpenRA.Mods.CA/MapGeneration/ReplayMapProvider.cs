using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Network;

namespace OpenRA.Mods.CA.MapGeneration
{
	// The complete archive is independent of future generator changes. Existing
	// recordings without an archive can still use their bounded lobby recipe.
	public sealed class ReplayMapProvider : IReplayMapProvider
	{
		const long MaxExpandedBytes = 64 * 1024 * 1024;
		const int MaxEntries = 256;

		public byte[] Capture(ModData mod, string mapUid)
		{
			var package = mod.MapCache[mapUid].Package;
			using var output = new MemoryStream();
			using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
			{
				long total = 0;
				var count = 0;
				// Preserve entry order: it participates in OpenRA's map UID.
				foreach (var name in package.Contents)
				{
					if (++count > MaxEntries || !SafeName(name)) throw new InvalidDataException("Unsupported replay map entry.");
					using var input = package.GetStream(name);
					total += input.Length;
					if (total > MaxExpandedBytes) throw new InvalidDataException("Replay map exceeds expanded size limit.");
					using var destination = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
					input.CopyTo(destination);
				}
			}

			var bytes = output.ToArray();
			ValidateArchive(bytes, mapUid);
			return bytes;
		}

		static bool SafeName(string name) => !string.IsNullOrEmpty(name) && !name.Contains('\\') && !name.Contains(':') &&
			!name.StartsWith("/", StringComparison.Ordinal) && name.Split('/').All(p => p != ".." && p != "." && p.Length > 0);

		internal static void ValidateArchive(byte[] bytes, string expectedUid)
		{
			if (bytes.Length == 0 || bytes.Length > ReplayMetadata.MaxEmbeddedMapBytes) throw new InvalidDataException("Invalid map archive size.");
			using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
			{
				if (zip.Entries.Count > MaxEntries) throw new InvalidDataException("Too many map archive entries.");
				long total = 0;
				var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var buffer = new byte[8192];
				foreach (var entry in zip.Entries)
				{
					if (!SafeName(entry.FullName) || !names.Add(entry.FullName) || entry.Length > MaxExpandedBytes)
						throw new InvalidDataException("Unsafe map archive entry.");
					total += entry.Length;
					if (total > MaxExpandedBytes) throw new InvalidDataException("Map archive expands beyond its limit.");
					using var input = entry.Open();
					long actual = 0;
					int count;
					while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
					{
						actual += count;
						if (actual > entry.Length) throw new InvalidDataException("Map archive entry length mismatch.");
					}
					if (actual != entry.Length) throw new InvalidDataException("Truncated map archive entry.");
				}
			}

			using var package = new ZipFileLoader.ReadOnlyZipFile(new MemoryStream(bytes), "replay.oramap");
			if (Map.ComputeUID(package) != expectedUid) throw new InvalidDataException("Map archive UID does not match the expected map.");
		}

		public bool Restore(ModData mod, ReplayMetadata replay)
		{
			if (replay.GameInfo.Mod != mod.Manifest.Id) return false;
			var bytes = replay.EmbeddedMap;
			var source = bytes == null ? "legacy recipe" : "embedded archive";
			if (bytes == null)
			{
				var recipe = ReadLegacyRecipe(replay.FilePath, replay.GameInfo.MapUid);
				if (recipe == null) return false;
				var options = GeneratedMapRecipe.Decode(recipe);
				var temporary = Path.Combine(Path.GetTempPath(), "ymca-replay-" + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(temporary);
				try
				{
					var path = new RubberduckMapExporter(mod).Export(MapPlanGenerator.Generate(options), options, temporary, temporary: true);
					bytes = File.ReadAllBytes(path);
				}
				finally { Directory.Delete(temporary, true); }
			}

			// No extraction into arbitrary paths, no loading rules before archive/UID validation.
			ValidateArchive(bytes, replay.GameInfo.MapUid);
			var folder = mod.MapCache.MapLocations.First(p => p.Value == MapClassification.User);
			var filename = TemporaryGeneratedMapFiles.NewFilename();
			var installed = Path.Combine(folder.Key.Name, filename);
			File.WriteAllBytes(installed, bytes);
			TemporaryGeneratedMaps.Register(installed);
			mod.MapCache.LoadMap(filename, folder.Key, folder.Value, mod.Manifest.Get<MapGrid>(), null);
			Log.Write("debug", "Restored replay map from " + source + ": " + replay.GameInfo.MapUid);
			return mod.MapCache[replay.GameInfo.MapUid].Status == MapStatus.Available;
		}

		internal static string ReadLegacyRecipe(string path, string expectedUid)
		{
			using var input = File.OpenRead(path);
			using var reader = new BinaryReader(input);
			Session.Global settings = null;
			// Only pre-game orders are needed. Bound both scanned bytes and packets.
			for (var i = 0; i < 10000 && input.Position + 8 <= input.Length && input.Position + 8 <= MaxExpandedBytes; i++)
			{
				if (reader.ReadInt32() == ReplayMetadata.MetaStartMarker) break;
				var length = reader.ReadInt32();
				if (length < 4 || length > 1024 * 1024 || length > Math.Min(input.Length, MaxExpandedBytes) - input.Position)
					throw new InvalidDataException("Invalid replay lobby packet.");
				var packet = reader.ReadBytes(length);
				if (BitConverter.ToInt32(packet, 0) != 0 || !OrderIO.TryParseOrderPacket(packet, out var orders)) continue;
				foreach (var order in orders.Orders.GetOrders(null))
				{
					if (order.OrderString == "SyncInfo") settings = Session.Deserialize(order.TargetString, order.OrderString).GlobalSettings;
					else if (order.OrderString == "SyncLobbyGlobalSettings")
					{
						foreach (var node in MiniYaml.FromString(order.TargetString, order.OrderString))
							if (node.Key.Split('@')[0] == "GlobalSettings") settings = Session.Global.Deserialize(node.Value);
					}
					else if (order.OrderString == "StartGame")
					{
						var selection = settings?.OptionOrDefault(GeneratedMapRecipe.Selection, "");
						var prefix = expectedUid + ":";
						return settings?.Map == expectedUid && settings.OptionOrDefault(GeneratedMapRecipe.Capability, "") == GeneratedMapRecipe.Version &&
							selection.StartsWith(prefix, StringComparison.Ordinal) ? selection.Substring(prefix.Length) : null;
					}
				}
			}

			return null;
		}
	}
}
