using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSovietSamArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-sam-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Resolve Soviet SAM pass12 sprites, construction and 32 muzzle projections.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["sam"];
			var traits = actor.TraitsInConstructOrder().ToArray();
			if (traits.Any(t => t.GetType().Name.Contains("EmbeddedTurret")) || !actor.HasTraitInfo<WithSpriteBodyInfo>() || !actor.HasTraitInfo<WithSpriteTurretInfo>())
				throw new InvalidDataException("SAM must use separate base and turret sprites.");
			var image = actor.TraitInfo<RenderSpritesInfo>().Image;
			if (image != "sam.soviet.native") throw new InvalidDataException("SAM image binding.");
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == image);
			var names = new[] { "idle", "damaged-idle", "base", "damaged-base", "turret", "damaged-turret", "place", "dead", "construction", "muzzle-native", "icon" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
			cache.LoadReservations(mod);
			foreach (var name in names) sequences[name].ResolveSprites(cache);
			foreach (var item in new[] { ("base", 1), ("turret32", 32), ("place", 1) })
			{
				var path = "bits/soviet/buildings/sam-" + item.Item1 + ".png";
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 192 || f.Size.Height != 192)) throw new InvalidDataException(path);
			}
			if (sequences["turret"].Facings != 32 || sequences["turret"].Scale != .75f || sequences["base"].Scale != .75f || sequences["construction"].Length != 10)
				throw new InvalidDataException("SAM sequence layout.");
			var arm = actor.TraitInfo<ArmamentInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var body = actor.TraitInfo<BodyOrientationInfo>();
			if (arm.Weapon != "Nike" || arm.MuzzleSequence != "muzzle-native" || turret.Offset.Z != 726) throw new InvalidDataException("SAM armament/mount.");
			var elevation = 34 * Math.PI / 180;
			var forward = Math.Cos(elevation) * 1.74 - Math.Sin(elevation) * .57;
			var height = 1.9 + 1.05 + Math.Cos(elevation) * .57 + Math.Sin(elevation) * 1.74;
			double error = 0;
			for (var i = 0; i < 32; i++)
			{
				var angle = (20 + i) * Math.PI / 16;
				var x = .66 * Math.Cos(angle) + forward * Math.Sin(angle);
				var y = .66 * Math.Sin(angle) - forward * Math.Cos(angle);
				var world = body.LocalToWorld(turret.Offset + arm.LocalOffset[0].Rotate(WRot.FromYaw(new WAngle(i * 32))));
				var dx = world.X * 64.0 / 1448 - 19.5 * (x + y) / Math.Sqrt(2);
				var dy = (world.Y - world.Z) * 64.0 / 1448 - (19.5 * (x - y) / Math.Sqrt(8) - 19.5 * height * Math.Sqrt(3) / 2);
				error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
			}
			if (error > .6) throw new InvalidDataException("SAM muzzle registration: " + error);
			Console.WriteLine($"Soviet SAM pass12: 75% base/32 facings, damaged/placement/construction sprites loaded; muzzle max error {error:F3}px. Live review pending.");
		}
	}
}
