using System;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckScorchPalettesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-scorch-palettes";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("Load all smudge palettes/sequences and export every scorch/crater/flame frame for review.", "OUTPUTDIR")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var world = mod.DefaultRules.Actors["world"];
			var palettes = world.TraitInfos<IProvidesCursorPaletteInfo>().ToArray();
			string Name(IProvidesCursorPaletteInfo p) => (string)p.GetType().GetField("Name")?.GetValue(p);
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).ToDictionary(n => n.Key);
			var smudges = palettes.Where(p => Name(p) == "legacy-smudge").ToArray();
			if (smudges.Length != 9) throw new InvalidDataException("Expected all nine tilesets.");
			foreach (var palette in smudges)
			{
				var tileset = ((ITilesetSpecificPaletteInfo)palette).Tileset;
				var loaded = palette.ReadPalette(mod.DefaultFileSystem);
				var extension = tileset.Contains("TEMPERATE") || tileset == "INTERIOR" ? "tem" :
					tileset == "SNOW" ? "sno" : tileset == "DESERT" ? "des" : tileset == "BARREN" ? "bar" : tileset == "JUNGLE" ? "jun" : "win";
				using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
				var sets = new[] { "scorches", "craters", "scorch_flames" }.Select(n => mod.SpriteSequenceLoader.ParseSequences(mod, tileset, cache, nodes[n])).ToArray();
				cache.LoadReservations(mod);
				foreach (var set in sets)
					foreach (var sequence in set.Values)
					{
						sequence.ResolveSprites(cache);
						for (var f = 0; f < sequence.Length; f++) _ = sequence.GetSprite(f);
					}
				foreach (var prefix in new[] { "sc", "cr" })
					for (var i = 1; i <= 6; i++) Export(mod, prefix + i + "." + extension, loaded, Path.Combine(args[1], tileset));
				foreach (var layer in world.TraitInfos<SmudgeLayerInfo>())
				{
					if (layer.Palette != "legacy-smudge") throw new InvalidDataException("Smudge uses terrain palette.");
					var smoke = palettes.Single(p => Name(p) == layer.SmokePalette);
					_ = smoke.ReadPalette(mod.DefaultFileSystem);
				}
				Console.WriteLine(tileset + ": every scorch/crater/combined flame frame resolved; ground and smoke palettes loaded.");
			}
			foreach (var name in new[] { "effect", "scorch-black" })
			{
				var palette = palettes.Single(p => Name(p) == name).ReadPalette(mod.DefaultFileSystem);
				foreach (var file in new[] { "fire1.shp", "fire2.shp", "fire3.shp", "fire4.shp", "smoke_m.shp" })
					Export(mod, file, palette, Path.Combine(args[1], name));
			}
		}
		static void Export(ModData mod, string file, IPalette palette, string directory)
		{
			Directory.CreateDirectory(directory);
			using var stream = mod.DefaultFileSystem.Open(file);
			var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, file, out _);
			var colors = Enumerable.Range(0, 256).Select(palette.GetColor).ToArray();
			for (var i = 0; i < frames.Length; i++)
				new Png(frames[i].Data, SpriteFrameType.Indexed8, frames[i].Size.Width, frames[i].Size.Height, colors)
					.Save(Path.Combine(directory, file + "-" + i + ".png"));
		}
	}
}
