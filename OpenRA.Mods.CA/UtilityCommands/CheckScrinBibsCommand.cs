using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckScrinBibsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-scrin-bibs";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Resolve installed native Scrin plating, ground-surface anchors and actor variants.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var binding = JObject.Parse(File.ReadAllText("art-source/scrin bib/integration/binding.json"));
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			foreach (var n in new[] { 1, 2, 3 })
			{
				var name = "scrinbib" + n + "x" + n;
				var file = "bits/scrin/buildings/bib" + n + "x" + n + ".png";
				var node = nodes.Single(s => s.Key == name);
				using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders,
					mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, node);
				cache.LoadReservations(mod);
				var sequence = sequences["idle"];
				sequence.ResolveSprites(cache);
				var sprite = sequence.GetSprite(0);
				if (sequence.Scale != 1 || sequence.Length != 1 || sprite.Offset.X != 0 || sprite.Offset.Y != 10)
					throw new InvalidDataException(name + ": surface anchor/scale mismatch.");
				using var raw = mod.DefaultFileSystem.Open(file);
				var png = new Png(raw);
				using var input = mod.DefaultFileSystem.Open(file);
				var frames = FrameLoader.GetFrames(input, mod.SpriteLoaders, file, out _);
				if (frames.Length != 1 || frames[0].Type == SpriteFrameType.Indexed8 || !frames[0].Data.SequenceEqual(png.Data))
					throw new InvalidDataException(name + ": source RGBA mismatch.");
				Console.WriteLine(name + " -> " + file + "; native RGBA, top-surface origin resolved.");
			}
			foreach (var entry in ((JObject)binding["actors"]).Properties())
			{
				var actor = mod.DefaultRules.Actors[entry.Name.ToLowerInvariant()];
				_ = actor.TraitsInConstructOrder().ToArray();
				var overlays = actor.TraitInfos<WithIdleOverlayInfo>().Where(o => o.Image != null && o.Image.StartsWith("scrinbib", StringComparison.Ordinal)).ToArray();
				if (overlays.Length != 1 || overlays[0].Image != (string)entry.Value["image"] || overlays[0].Palette != "effect" || actor.HasTraitInfo<WithBuildingBibInfo>())
					throw new InvalidDataException(entry.Name + ": missing, duplicate or legacy bib.");
			}
			Console.WriteLine("Scrin bibs and inherited variants resolved; live grid alignment remains a separate review.");
		}
	}
}
