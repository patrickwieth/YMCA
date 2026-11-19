using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class GeneratedMapInstallation
	{
		// A peer host shares ModData with its client. Remember immutable, bounded
		// inputs for maps generated before the server exists, without retaining old mods.
		static readonly ConditionalWeakTable<ModData, ConcurrentDictionary<string, string>> Recipes = new();

		public static bool TryGetRecipe(ModData mod, string uid, out string recipe)
		{
			recipe = null;
			return !string.IsNullOrEmpty(uid) && Recipes.TryGetValue(mod, out var known) && known.TryGetValue(uid, out recipe);
		}

		public static void InstallArchive(ModData mod, byte[] bytes, string uid)
		{
			if (mod.MapCache[uid].Status == MapStatus.Available) return;
			ReplayMapProvider.ValidateArchive(bytes, uid);
			var folder = mod.MapCache.MapLocations.First(p => p.Value == MapClassification.User);
			var filename = TemporaryGeneratedMapFiles.NewFilename();
			var path = Path.Combine(folder.Key.Name, filename);
			var owned = false;
			try
			{
				using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
				{
					owned = true;
					file.Write(bytes, 0, bytes.Length);
				}
				TemporaryGeneratedMaps.Register(path);
				mod.MapCache.LoadMap(filename, folder.Key, folder.Value, mod.Manifest.Get<MapGrid>(), null);
				if (mod.MapCache[uid].Status != MapStatus.Available) throw new InvalidDataException("Downloaded map could not be loaded.");
			}
			catch
			{
				if (owned)
				{
					if (mod.MapCache[uid].PackageName == path) mod.MapCache[uid].DisposePackage();
					try { File.Delete(path); } catch (IOException e) { Log.Write("debug", e); }
				}
				throw;
			}
		}

		public static string Install(ModData mod, MapPlan plan, MapGenerationOptions options, bool clientCleanup = true)
		{
			var folder = mod.MapCache.MapLocations.First(p => p.Value == MapClassification.User);
			var path = new RubberduckMapExporter(mod).Export(plan, options, folder.Key.Name, temporary: true);
			if (clientCleanup) TemporaryGeneratedMaps.Register(path);
			mod.MapCache.LoadMap(Path.GetFileName(path), folder.Key, folder.Value, mod.Manifest.Get<MapGrid>(), null);
			var map = mod.MapCache.FirstOrDefault(m => m.Status == MapStatus.Available &&
				string.Equals(Path.GetFullPath(m.PackageName), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
			if (map == null) throw new InvalidDataException("Generated map could not be loaded.");
			try { Recipes.GetOrCreateValue(mod)[map.Uid] = GeneratedMapRecipe.Encode(options); }
			catch (ArgumentException e)
			{
				// Preserve locally supported settings beyond the bounded network protocol.
				Log.Write("debug", "Generated map cannot be synchronized by recipe: " + e.Message);
			}
			return map.Uid;
		}
	}
}
