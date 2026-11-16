using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSovietKennelArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-kennel-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Load supplied native kennel PNG, resolved construction and damaged sequences.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["kenn"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "kenn.soviet")
				throw new InvalidDataException("Kennel image not bound.");
			const string Path = "bits/soviet/buildings/kennel.png";
			using var stream = mod.DefaultFileSystem.Open(Path);
			var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, Path, out _);
			if (frames.Length != 1 || frames[0].Size.Width != 128 || frames[0].Size.Height != 128)
				throw new InvalidDataException("Supplied kennel must be one native128 frame.");
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == "kenn.soviet");
			var names = new[] { "Defaults", "idle", "damaged-idle", "construction" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			if (sequences["idle"].Length != 1 || sequences["damaged-idle"].Length != 1 || sequences["idle"].Scale != 1 || sequences["construction"].Length != 10)
				throw new InvalidDataException("Kennel animation/scale incorrect.");
			Console.WriteLine("Kennel: supplied native128 PNG, idle/damaged/construction sequences and actor dependencies loaded. Live dog-exit review pending.");
		}
	}
}
