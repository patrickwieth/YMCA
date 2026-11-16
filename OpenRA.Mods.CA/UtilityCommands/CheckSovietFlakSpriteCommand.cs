using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSovietFlakSpriteCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-flak-sprite";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Load the replacement Soviet flak sprites and verify muzzle registration.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["flak.soviet.sprite"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.HasTraitInfo<RenderVoxelsInfo>() || !actor.HasTraitInfo<WithSpriteBodyInfo>() || !actor.HasTraitInfo<WithSpriteTurretInfo>())
				throw new InvalidDataException("Flak must render only separate sprites.");
			foreach (var item in new[] { ("flak-base.png", 1, 192, 192), ("flak-turret32.png", 32, 192, 192) })
			{
				var path = "bits/soviet/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != item.Item3 || f.Size.Height != item.Item4))
					throw new InvalidDataException(path);
			}
			var image = actor.TraitInfo<RenderSpritesInfo>().Image;
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == image);
			var names = new[] { "Defaults", "idle", "damaged-idle", "base", "damaged-base", "turret", "damaged-turret", "construction", "muzzle-cluster" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			if (sequences["turret"].Facings != 32 || sequences["base"].Length != 1 || sequences["base"].Scale != 1 || sequences["turret"].Scale != 1)
				throw new InvalidDataException("Flak sprite layout.");
			var arm = actor.TraitInfo<ArmamentInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var grid = mod.Manifest.Get<MapGrid>();
			if (body.UseClassicPerspectiveFudge) throw new InvalidDataException("Isometric sprite flak must not double-fudge its128x64 projection.");
			if (arm.Recoil.Length != 0 || arm.Weapon != "SOVIET-CLUSTER-AA") throw new InvalidDataException("Unexpected flak armament.");
			if (arm.MuzzleSequence != "muzzle-cluster" || sequences["muzzle-cluster"].Length != 5 || !actor.TraitsInConstructOrder().Any(t => t.GetType().Name == "WithMuzzleOverlayInfo"))
				throw new InvalidDataException("Missing flak muzzle flash.");
			var forward = .10 + Math.Cos(42 * Math.PI / 180) * 3.84;
			var height = 2.14 + Math.Sin(42 * Math.PI / 180) * 3.84;
			// Approved matching modeled pedestal restores its native bearing height.
			if (turret.Offset.Z != 243) throw new InvalidDataException("Modeled pedestal mount.");
			double maxError = 0;
			for (var i = 0; i < 32; i++)
			{
				var angle = (20 + i) * Math.PI / 16;
				var x = forward * Math.Sin(angle); var y = -forward * Math.Cos(angle);
				var p = body.LocalToWorld(turret.Offset + arm.LocalOffset[0].Rotate(WRot.FromYaw(new WAngle(i * 32))));
				var dx = p.X * (double)grid.TileSize.Width / grid.TileScale - 20 * (x + y) / Math.Sqrt(2);
				var dy = (p.Y - p.Z) * (double)grid.TileSize.Height / grid.TileScale - (20 * (x - y) / Math.Sqrt(8) - 20 * height * Math.Sqrt(3) / 2);
				maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
			}
			if (maxError > .5) throw new InvalidDataException("Flak muzzle error: " + maxError);
			Console.WriteLine($"Soviet flak: approved pass15 modeled pedestal + native32 turret loaded, construction/damaged states valid; actual-grid muzzle max {maxError:F3}px. No separate recoil. Live review pending.");
		}
	}
}
