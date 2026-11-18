using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckBibConnectionsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-bib-connections";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		[Desc("Resolve bib connectors and faction color variants, including grid/mask registrations and owner palettes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders,
				mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var node in nodes.Where(n => n.Key.StartsWith("sovietbib") || n.Key.StartsWith("alliedbib") ||
				n.Key.StartsWith("gdibib") || n.Key.StartsWith("nodbib") || n.Key == "scrinbib1x1"))
			{
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, node);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values)
					sequence.ResolveSprites(cache);
				var original = sequences["idle"];
				if (node.Key.StartsWith("gdibib"))
				{
					var size = node.Key.Substring(6).Split('.')[0];
					var expected = "bits/gdi/buildings/bib" + size + (size == "1x1" ? ".png" : "-grid.png");
					var filename = node.Value.Nodes.Single(n => n.Key == "idle").Value.Nodes.Single(n => n.Key == "Filename").Value.Value;
					if (filename != expected) throw new InvalidDataException("Unexpected GDI foundation binding: " + filename);
					Console.WriteLine(node.Key + " runtime file: " + filename);
				}
				if (node.Key.StartsWith("sovietbib"))
				{
					var mask = sequences["remap"];
					var grid = sequences["grid"];
					if (grid.GetSprite(0).Size != original.GetSprite(0).Size || grid.GetSprite(0).Offset != original.GetSprite(0).Offset || grid.Scale != original.Scale)
						throw new InvalidDataException("Grid foundation registration mismatch: " + node.Key);
					if (mask.GetSprite(0).Size != original.GetSprite(0).Size || mask.GetSprite(0).Offset != original.GetSprite(0).Offset ||
						mask.Scale != original.Scale || mask.GetSprite(0).Channel == TextureChannel.RGBA)
						throw new InvalidDataException("Bib mask registration/palette mismatch: " + node.Key);
				}

				if (node.Key.StartsWith("gdibib") || node.Key.StartsWith("nodbib"))
				{
					var mask = sequences["remap"];
					if (mask.GetSprite(0).Size != original.GetSprite(0).Size || mask.GetSprite(0).Offset != original.GetSprite(0).Offset ||
						mask.Scale != original.Scale || mask.GetSprite(0).Channel == TextureChannel.RGBA)
						throw new InvalidDataException("Color variant registration mismatch: " + node.Key);
				}

				if (node.Key.StartsWith("alliedbib"))
				{
					var mask = sequences["remap"];
					if (mask.GetSprite(0).Size != original.GetSprite(0).Size || mask.GetSprite(0).Offset != original.GetSprite(0).Offset ||
						mask.Scale != original.Scale || mask.GetSprite(0).Channel == TextureChannel.RGBA)
						throw new InvalidDataException("Allied rim registration mismatch: " + node.Key);
				}

				Console.WriteLine(node.Key + " resolved; offset=" + original.GetSprite(0).Offset);
			}

			var rules = mod.DefaultRules.Actors;
			foreach (var actor in rules.Values.Where(a => !a.Name.StartsWith("^")))
				foreach (var family in new[] { "gdibib", "nodbib" })
					if (actor.TraitInfos<WithIdleOverlayInfo>().Any(b => b.Image?.StartsWith(family) == true))
					{
						var remap = actor.TraitInfoOrDefault<WithBibRemapInfo>();
						if (remap == null || remap.ImagePrefix != family || remap.Palette != family + "player" ||
							remap.RemapConnections != (family == "nodbib"))
							throw new InvalidDataException("Incorrect color variant: " + actor.Name);
						_ = actor.TraitsInConstructOrder().ToArray();
						Console.WriteLine("BIB_VARIANT " + actor.Name + " " + family);
					}
			foreach (var name in new[] { "world", "editorworld" })
				if (!rules[name].HasTraitInfo<BibConnectionLayerInfo>())
					throw new InvalidDataException("Missing connection layer: " + name);
			foreach (var name in new[] { "fact.soviet", "powr.soviet", "ftur", "ttur", "sam", "fix.soviet", "kenn" })
			{
				if (!rules[name].HasTraitInfo<WithBibRemapInfo>() || rules[name].TraitInfo<WithBibRemapInfo>().ImagePrefix != "sovietbib" ||
					rules[name].TraitInfo<WithBibRemapInfo>().Palette != "bibplayer")
					throw new InvalidDataException("Missing Soviet remap: " + name);
				_ = rules[name].TraitsInConstructOrder().ToArray();
			}

			foreach (var name in new[] { "fact", "powr", "apwr", "proc", "weap", "tent", "fix", "hpad" })
			{
				var actor = rules[name];
				var rim = actor.TraitInfoOrDefault<WithBibRemapInfo>();
				if (rim == null || rim.ImagePrefix != "alliedbib" || rim.Palette != "alliedbibplayer")
					throw new InvalidDataException("Missing Allied rim remap: " + name);
				_ = actor.TraitsInConstructOrder().ToArray();
			}

			foreach (var actor in rules.Values.Where(a => !a.Name.StartsWith("^") && a.HasTraitInfo<WithBibRemapInfo>()))
			{
				if (actor.TraitInfos<WithIdleOverlayInfo>().Any(b => b.Image?.StartsWith("sovietbib") == true && b.Sequence != "grid"))
					throw new InvalidDataException("Soviet remap actor missing fixed grid: " + actor.Name);
				Console.WriteLine("Remap actor: " + actor.Name);
			}
			foreach (var actor in rules.Values.Where(a => a.Name.StartsWith("ch") &&
				a.TraitInfos<WithIdleOverlayInfo>().Any(b => b.Image?.StartsWith("sovietbib") == true)))
			{
				var remap = actor.TraitInfoOrDefault<WithBibRemapInfo>();
				if (remap == null || remap.ImagePrefix != "sovietbib" || remap.Palette != "bibplayer")
					throw new InvalidDataException("Missing China Soviet-style bib remap: " + actor.Name);
				_ = actor.TraitsInConstructOrder().ToArray();
				Console.WriteLine("China grid/remap verified: " + actor.Name);
			}
			foreach (var name in new[] { "sfac", "reac", "port" })
				if (rules.TryGetValue(name, out var actor) && actor.HasTraitInfo<WithBibRemapInfo>() &&
					actor.TraitInfos<WithIdleOverlayInfo>().Any(b => b.Image?.StartsWith(actor.TraitInfo<WithBibRemapInfo>().ImagePrefix) == true))
					throw new InvalidDataException("Unexpected faction remap: " + name);
			Console.WriteLine("Bib connectors and Soviet/China joints, Allied rims, GDI corners and Nod lower-edge masks resolved.");
		}
	}
}
