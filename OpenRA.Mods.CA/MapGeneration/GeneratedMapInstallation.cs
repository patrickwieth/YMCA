using System;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class GeneratedMapInstallation
	{
		public static string Install(ModData mod, MapPlan plan, MapGenerationOptions options, bool clientCleanup = true)
		{
			var folder = mod.MapCache.MapLocations.First(p => p.Value == MapClassification.User);
			var path = new RubberduckMapExporter(mod).Export(plan, options, folder.Key.Name, temporary: true);
			if (clientCleanup) TemporaryGeneratedMaps.Register(path);
			mod.MapCache.LoadMap(Path.GetFileName(path), folder.Key, folder.Value, mod.Manifest.Get<MapGrid>(), null);
			var map = mod.MapCache.FirstOrDefault(m => m.Status == MapStatus.Available &&
				string.Equals(Path.GetFullPath(m.PackageName), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
			if (map == null) throw new InvalidDataException("Generated map could not be loaded.");
			return map.Uid;
		}
	}
}
