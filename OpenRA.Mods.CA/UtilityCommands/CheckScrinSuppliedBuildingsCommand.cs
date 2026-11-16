using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckScrinSuppliedBuildingsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-scrin-supplied-buildings";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Resolve and load the supplied Scrin buildings and Headquarter, including the dedicated Tech Center PNG.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			var specs = new[] {
				("sfac", "headquarter.png", 1.25f, -27),
				("rea2", "advanced power.png", 1.25f, -27),
				("reac", "power plant.png", 1f, -28), ("proc.scrin", "refinery.png", 1.25f, -32),
				("port", "portal.png", 1f, -19), ("wsph", "warp sphere.png", 1.25f, -36),
				("nerv", "nerve center.png", 1f, -45), ("scrt", "tech center.png", 1f, -56),
				("grav", "gravity stabilizer.png", 1.25f, -34), ("sign", "signal transmitter.png", 1.25f, -31) };
			foreach (var spec in specs)
			{
				var actor = mod.DefaultRules.Actors[spec.Item1];
				_ = actor.TraitsInConstructOrder().ToArray();
				var image = actor.TraitInfo<RenderSpritesInfo>().Image;
				if (image != spec.Item1 + ".supplied" || actor.HasTraitInfo<WithBuildingBibInfo>())
					throw new InvalidDataException(spec.Item1 + ": binding/bib regression.");
				var path = "bits/scrin/buildings/" + spec.Item2;
				var indexed = spec.Item1 != "sfac" && spec.Item1 != "rea2" && spec.Item1 != "scrt";
				if (indexed)
				{
					var body = actor.TraitInfo<WithSpriteBodyInfo>();
					var paletteName = "scrin-source-" + spec.Item1.Replace('.', '-');
					if (body.Palette != paletteName || body.IsPlayerPalette)
						throw new InvalidDataException(image + ": indexed source must use its own embedded palette.");
					using (var source = mod.DefaultFileSystem.Open(path))
					{
						var png = new Png(source);
						foreach (var world in new[] { "world", "editorworld" })
						{
							var provider = mod.DefaultRules.Actors[world].TraitInfos<IProvidesCursorPaletteInfo>().Single(p =>
								(string)p.GetType().GetField("Name")?.GetValue(p) == paletteName);
							var palette = provider.ReadPalette(mod.DefaultFileSystem);
							if (png.Palette == null || png.Data.Any(index => palette[index] != png.Palette[index].ToArgb()))
								throw new InvalidDataException(image + ": embedded RGBA palette differs in " + world);
						}
					}
					var death = actor.TraitInfos<WithDeathAnimationInfo>().SingleOrDefault();
					if (death != null && (death.DeathSequencePalette != paletteName || death.DeathPaletteIsPlayerPalette))
						throw new InvalidDataException(image + ": death palette mismatch.");
				}
				using (var stream = mod.DefaultFileSystem.Open(path))
				{
					var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
					if (frames.Length != 1 || frames[0].Size.Width != 256 || frames[0].Size.Height != 256 ||
						(frames[0].Type == SpriteFrameType.Indexed8) != indexed)
						throw new InvalidDataException(path + ": unexpected source frame.");
				}
				var node = nodes.Single(n => n.Key == image);
				var states = spec.Item1 == "sfac" ? new[] { "idle", "damaged-idle", "place", "dead", "build", "damaged-build", "pdox", "damaged-pdox" } : new[] { "idle", "damaged-idle", "place", "dead" };
				var names = states.Concat(new[] { "Defaults", "construction", "icon" }).ToArray();
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
				using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
				foreach (var state in states)
				{
					var filename = node.Value.Nodes.Single(n => n.Key == state).Value.Nodes.Single(n => n.Key == "Filename").Value.Value;
					var sequence = sequences[state];
					var sprite = sequence.GetSprite(0);
					if (filename != path || sequence.Length != 1 || sequence.Scale != spec.Item3 ||
						sprite.Offset.X != 0 || sprite.Offset.Y != spec.Item4 - 64)
						throw new InvalidDataException(image + "/" + state + ": resolved source/placement mismatch.");
				}
				if (sequences["construction"].Length != (spec.Item1 == "proc.scrin" ? 20 : 15))
					throw new InvalidDataException(image + ": shared construction changed.");
				Console.WriteLine(spec.Item1 + " -> " + path + (!indexed ? "; native RGBA; " : "; embedded source RGBA verified in world/editor; ") + "native/damaged/placement/death/icon/construction loaded.");
			}
		}
	}
}
