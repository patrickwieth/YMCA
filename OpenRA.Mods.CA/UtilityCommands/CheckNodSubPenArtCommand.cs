using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckNodSubPenArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-nod-sub-pen-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Load the supplied Nod Sub Pen PNG, construction and legacy icon.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["spen.nod"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (!actor.TraitInfo<RenderSpritesInfo>().Image.Equals("spennod", StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("Nod Sub Pen image binding.");
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			var node = nodes.Single(n => n.Key == "spennod");
			var names = new[] { "idle", "damaged-idle", "place", "construction", "icon" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			foreach (var name in new[] { "idle", "damaged-idle", "place" })
				if (sequences[name].Length != 1 || sequences[name].GetSprite(0).Size != new float3(300, 256, 0))
					throw new InvalidDataException("Sub Pen native PNG dimensions.");
			Console.WriteLine("Nod Sub Pen: native300x256 PNG states, construction/icon and actor dependencies loaded. Live naval exit review pending.");
		}
	}
}
