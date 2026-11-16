using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckNodWarFactoryCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-nod-war-factory";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Resolve Nod factory/retired airstrip dependencies and load actual sprite states.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			foreach (var name in new[] { "weap.nod", "weap.td", "airs" })
				_ = mod.DefaultRules.Actors[name].TraitsInConstructOrder().ToArray();
			var nod = mod.DefaultRules.Actors["weap.nod"];
			if (nod.TraitInfo<RenderSpritesInfo>().Image != "weap.nod.png" || !nod.TraitInfo<BuildableInfo>().Prerequisites.SequenceEqual(new[] { "proc.td", "~structures.nod" }))
				throw new InvalidDataException("Nod factory image/build gate.");
			if (!mod.DefaultRules.Actors["airs"].TraitInfo<BuildableInfo>().Prerequisites.Contains("~disabled") || mod.DefaultRules.Actors["airs"].TraitInfo<RenderSpritesInfo>().Image != "astrip")
				throw new InvalidDataException("Airstrip must use its legacy art and be disabled for new construction.");
			if (nod.TraitsInConstructOrder().Any(t => t.GetType().Name == "ProductionAirdropCAInfo"))
				throw new InvalidDataException("Nod factory must use ground production, not the airstrip's airdrop.");
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var image in new[] { "weap.nod.png", "astrip", "nodbib4x3" })
			{
				var node = nodes.Single(n => n.Key == image);
				var names = new[] { "Defaults", "idle", "damaged-idle", "active", "damaged-active", "dead", "place", "construction", "icon" };
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
				if (image == "nodbib4x3" && (sequences["idle"].Scale != 1 || sequences["idle"].GetSprite(0).Size.X != 448 || sequences["idle"].GetSprite(0).Size.Y != 231 || sequences["idle"].GetSprite(0).Offset.X != 32))
					throw new InvalidDataException("Nod4x3 bib native sizing/anchor mismatch.");
				if (image == "weap.nod.png" && (sequences["idle"].Length != 1 || sequences["construction"].Length != 15))
					throw new InvalidDataException("Nod factory PNG/construction frames.");
			}
			Console.WriteLine("Nod factory: independent PNG binding, ground production and actor dependencies loaded; legacy airstrip disabled/restored; factory, airstrip and bib sprite states loaded. Live vehicle exit review pending.");
		}
	}
}
