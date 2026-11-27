using System;
using System.Linq;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class ListBuildingConstructionCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--list-building-construction";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || args.Length == 2 && args[1] == "--check";
		[Desc("List resolved building make sequences and footprint dimensions.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var nodes = args.Length == 2 ? MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).ToDictionary(n => n.Key) : null;
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var pair in mod.DefaultRules.Actors.OrderBy(p => p.Key))
			{
				var actor = pair.Value;
				if (pair.Key.StartsWith("^") || !actor.HasTraitInfo<BuildingInfo>() || !actor.HasTraitInfo<WithMakeAnimationInfo>() || !actor.HasTraitInfo<RenderSpritesInfo>() || actor.TraitInfo<WithMakeAnimationInfo>().Condition != "build-incomplete")
					continue;
				var building = actor.TraitInfo<BuildingInfo>();
				var image = actor.TraitInfo<RenderSpritesInfo>().Image ?? pair.Key;
				var name = actor.TraitInfo<WithMakeAnimationInfo>().Sequence;
				if (nodes != null)
				{
					_ = actor.TraitsInConstructOrder().ToArray();
					if (name != "construction")
						throw new InvalidDataException(pair.Key + ": obsolete make sequence still selected.");
					var node = nodes[image.ToLowerInvariant()];
					var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => n.Key == "Defaults" || n.Key == name).ToArray()));
					var sequence = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected)[name];
					cache.LoadReservations(mod);
					sequence.ResolveSprites(cache);
					// Footprints have been refined independently of the approved animation timing.
					// Validate the selected stage family, rather than silently resizing animations.
					var firstFrame = node.Value.Nodes.First(n => n.Key == name).Value.Nodes.First(n => n.Key == "Combine")
						.Value.Nodes.First().Value.Nodes.First(n => n.Key == "Filename").Value.Value;
					var size = firstFrame switch
					{
						"make-small1.png" => "small",
						"make-medium1.png" => "medium",
						"make-big1.png" => "big",
						_ => throw new InvalidDataException(pair.Key + ": unknown construction stage family.")
					};
					var stages = size == "small" ? 2 : size == "medium" ? 3 : 4;
					if (sequence.Length != stages * 5 || sequence.Facings != 1 || sequence.Scale != 1)
						throw new InvalidDataException(pair.Key + ": invalid construction sequence.");
					for (var i = 1; i <= stages; i++)
					{
						var path = "make-" + size + i + ".png";
						using var stream = mod.DefaultFileSystem.Open(path);
						if (FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _).Length != 1)
							throw new InvalidDataException(path);
					}
				}
				Console.WriteLine($"{pair.Key}\t{image}\t{building.Dimensions.X}\t{building.Dimensions.Y}\t{name}");
			}
		}
	}
}
