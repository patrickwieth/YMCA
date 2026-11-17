using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSovietBibAlignmentCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-bib-alignment";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Resolve Soviet conyard/power body sprites and unchanged native bib registrations.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders,
				mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var spec in new[] {
				("fact.soviet", new float2(-1, -52.75f)),
				("powr.soviet", new float2(0, -71)),
				("sovietbib4x4", float2.Zero),
				("sovietbib3x3", float2.Zero) })
			{
				var node = nodes.Single(n => n.Key == spec.Item1);
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null,
					node.Value.Nodes.Where(n => n.Key == "Defaults" || n.Key == "idle" || n.Key == "damaged-idle").ToArray()));
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values)
				{
					sequence.ResolveSprites(cache);
					var offset = sequence.GetSprite(0).Offset;
					if (Math.Abs(offset.X - spec.Item2.X) > .001 || Math.Abs(offset.Y - spec.Item2.Y) > .001 ||
						Math.Abs(sequence.Scale - 1) > .001 || sequence.Length != 1)
						throw new InvalidDataException("Soviet body/bib registration mismatch: " + spec.Item1 + " / " + offset);
				}
			}
			foreach (var name in new[] { "fact.soviet", "fact.nukular.soviet", "powr.soviet" })
			{
				var actor = mod.DefaultRules.Actors[name];
				_ = actor.TraitsInConstructOrder().ToArray();
				var expected = name == "powr.soviet" ? "powr.soviet" : "fact.soviet";
				if (actor.TraitInfo<RenderSpritesInfo>().Image != expected)
					throw new InvalidDataException("Soviet actor image mismatch: " + name);
			}
			Console.WriteLine("Soviet conyard/Nukular and power plant: native body offsets resolved in idle/damaged states; original bib registration/scale retained. Live visual review pending.");
		}
	}
}
