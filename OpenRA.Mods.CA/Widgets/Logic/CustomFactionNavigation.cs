using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Modular;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public sealed class CustomFactionEditorSession
	{
		public CustomFactionRoster Roster;
		public int Selected;
		public bool NewDesign;
		public Action<CustomFactionRoster> Save;
	}

	public static class CustomFactionNavigation
	{
		public static CustomFactionDesign Compiler(ModData modData)
		{
			using var stream = modData.DefaultFileSystem.Open("ca|modular/designer-catalog.json");
			using var reader = new StreamReader(stream);
			return new CustomFactionDesign(reader.ReadToEnd());
		}

		// Same immutable test-map path as the vehicle designer; this is not arbitrary-map support.
		public static string PrepareGame(ModData modData, CustomFactionDesign compiler, CustomFactionRoster roster)
		{
			byte[] lab;
			using (var stream = modData.DefaultFileSystem.Open("ca|maps/modular-gdi-lab.oramap"))
			using (var memory = new MemoryStream()) { stream.CopyTo(memory); lab = memory.ToArray(); }
			var bytes = compiler.CompileRosterMap(roster, lab);
			var directory = Path.Combine(Platform.SupportDir, "maps", "ca", "modular");
			Directory.CreateDirectory(directory);
			var filename = "custom-" + CustomFactionDesign.ContentHash(bytes) + ".oramap";
			var path = Path.Combine(directory, filename);
			if (!File.Exists(path))
			{
				var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
				try { File.WriteAllBytes(temp, bytes); File.Move(temp, path); }
				finally { if (File.Exists(temp)) File.Delete(temp); }
			}
			else if (!File.ReadAllBytes(path).SequenceEqual(bytes))
				throw new InvalidDataException("Existing test map was modified; it will not be overwritten.");
			var location = modData.MapCache.MapLocations.Where(kv => kv.Value == MapClassification.User).Select(kv => kv.Key).First(p =>
				string.Equals(Path.GetFullPath(p.Name).TrimEnd(Path.DirectorySeparatorChar),
				Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
			string uid;
			using (var package = location.OpenPackage(filename, modData.ModFiles)) uid = Map.ComputeUID(package);
			modData.MapCache.LoadMap(filename, location, MapClassification.User, modData.Manifest.Get<MapGrid>(), null);
			if (modData.MapCache[uid].Status != MapStatus.Available) throw new InvalidDataException("The generated map could not be loaded.");
			return uid;
		}
	}
}
