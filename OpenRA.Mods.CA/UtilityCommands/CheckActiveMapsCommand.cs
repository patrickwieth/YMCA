using System;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.FileSystem;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckActiveMapsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-active-maps";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Validate the active tilesets and pinned shellmap, including stale installed and user shellmaps.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			if (mod.Manifest.ShellmapName != "liberty-stream-shellmap" ||
				mod.DefaultTerrainInfo.Keys.Any(k => !k.StartsWith("RUBBERDUCK-", StringComparison.Ordinal)))
				throw new InvalidDataException("Expected pinned Liberty and Rubberduck-only terrain.");
			mod.MapCache.LoadMaps();
			var current = mod.MapCache.Single(m => m.Status == MapStatus.Available &&
				m.Class == MapClassification.System && Path.GetFileName(m.PackageName) == mod.Manifest.ShellmapName);
			var expected = current.Uid;
			var choose = typeof(Game).GetMethod("ChooseShellmap", BindingFlags.NonPublic | BindingFlags.Static);
			var temporary = Path.Combine(Path.GetTempPath(), "ymca-stale-shellmap-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(temporary);
			try
			{
				void AddStale(string name, string title, bool oldTerrain, MapClassification classification)
				{
					var directory = Path.Combine(temporary, name);
					Directory.CreateDirectory(directory);
					foreach (var file in Directory.GetFiles(current.PackageName)) File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
					var yaml = Path.Combine(directory, "map.yaml");
					var text = File.ReadAllText(yaml).Replace("Title: Liberty Island - Kirov Assault", "Title: " + title);
					if (oldTerrain) text = text.Replace("Tileset: RUBBERDUCK-TEMPERATE", "Tileset: OMEGABOLT-TEMPERATE");
					File.WriteAllText(yaml, text);
					using var folder = new Folder(temporary);
					mod.MapCache.LoadMap(name, folder, classification, mod.Manifest.Get<MapGrid>(), null);
					var loaded = mod.MapCache.Single(m => m.Title == title);
					if (oldTerrain && loaded.Status != MapStatus.Unavailable)
						throw new InvalidDataException("Legacy tileset map remained available.");
				}

				AddStale("island-wars-II", "Island Wars II - Wasteland stale test", true, MapClassification.System);
				AddStale("other-shellmap", "Old compatible shellmap test", false, MapClassification.System);
				AddStale("liberty-stream-shellmap", "User shellmap with matching filename test", false, MapClassification.User);
				for (var i = 0; i < 100; i++)
					if ((string)choose.Invoke(null, null) != expected)
						throw new InvalidDataException("Stale shellmap bypassed the pinned system map.");
				Console.WriteLine("Active maps passed: Rubberduck-only, legacy maps unavailable, stale system/user shellmaps never selected.");
			}
			finally { Directory.Delete(temporary, true); }
		}
	}
}
