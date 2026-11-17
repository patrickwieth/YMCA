using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckNodSamArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-nod-sam-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check Nod SAM ammo art, burst barrel registration and sprite dependencies.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["nsam"];
			_ = actor.TraitsInConstructOrder().ToArray();
			var arm = actor.TraitInfo<ArmamentInfo>();
			var weapon = mod.DefaultRules.Weapons[arm.Weapon.ToLowerInvariant()];
			var reload = actor.TraitInfo<ReloadAmmoPoolInfo>();
			var ammo = actor.TraitInfo<AmmoPoolInfo>();
			var turrets = actor.TraitInfos<WithSpriteTurretInfo>().ToArray();
			if (weapon.Burst != 4 || weapon.ReloadDelay != 30 || !weapon.BurstDelays.SequenceEqual(new[] { 10 }) || ammo.Ammo != 4 || reload.Delay != 30 || reload.Count != 4 || !reload.ResetOnFire || turrets.Length != 5)
				throw new InvalidDataException("SAM burst/ammo configuration.");
			if (actor.TraitsInConstructOrder().Any(t => t.GetType().Name.Contains("Popup") || t.GetType().Name.Contains("EmbeddedTurret")))
				throw new InvalidDataException("Legacy popup/embedded turret still present.");
			if (turrets.Count(t => t.EnabledByDefault) != 1 || turrets.Single(t => t.EnabledByDefault).Sequence != "turret-4")
				throw new InvalidDataException("Preview must show exactly one fully loaded turret.");
			for (var count = 0; count <= 4; count++)
			{
				var conditions = new Dictionary<string, int> { ["sam-active"] = 1, ["sam-ammo"] = count, ["build-incomplete"] = 0 };
				var visible = turrets.Where(t => t.RequiresCondition.Evaluate(conditions)).ToArray();
				if (visible.Length != 1 || visible[0].Sequence != "turret-" + count)
					throw new InvalidDataException("Incorrect ammo-state visibility.");
				conditions["build-incomplete"] = 1;
				if (turrets.Any(t => t.RequiresCondition.Evaluate(conditions)))
					throw new InvalidDataException("Turret visible during construction.");
			}
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			var node = nodes.Single(n => n.Key == "nsam.native");
			var names = new[] { "idle", "damaged-idle", "base", "damaged-base", "place", "dead", "construction", "muzzle", "icon", "turret-0", "turret-1", "turret-2", "turret-3", "turret-4" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			for (var n = 0; n <= 4; n++)
				if (sequences["turret-" + n].Facings != 32 || sequences["turret-" + n].Length != 1 || sequences["turret-" + n].Scale != .6f)
					throw new InvalidDataException("Ammo state directions.");
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var grid = mod.Manifest.Get<MapGrid>();
			var mount = body.LocalToWorld(turret.Offset);
			var mountX = mount.X * (double)grid.TileSize.Width / grid.TileScale;
			var mountY = (mount.Y - mount.Z) * (double)grid.TileSize.Height / grid.TileScale;
			if (body.UseClassicPerspectiveFudge || Math.Abs(mountX + 7.8) > .1 || Math.Abs(mountY + 27) > .1)
				throw new InvalidDataException("SAM bearing registration must use actual rectangular-isometric grid projection.");
			var selection = actor.TraitInfo<IsometricSelectableInfo>();
			var bounds = new[] { 950, 977, 0, -55 };
			if (!selection.Bounds.SequenceEqual(bounds) || !selection.DecorationBounds.SequenceEqual(bounds) || selection.Height != 60 || selection.DecorationHeight != 60)
				throw new InvalidDataException("SAM selection must fit the fixed foundation, not the old skewed rectangle.");
			// In barrel-index order: actual Burst % barrelCount gives slots2,3,0,1.
			var tails = new[] { (0.44221738, -3.27064443), (-0.44166610, -1.32610083), (-0.68915349, -2.70495915), (0.68970478, -1.89178634) };
			double error = 0;
			for (var b = 0; b < 4; b++)
				for (var i = 0; i < 32; i++)
				{
					var angle = (4 + i) * Math.PI / 16;
					var x = tails[b].Item1; var y = tails[b].Item2 + 4.3 * Math.Cos(Math.PI / 10);
					var z = 2.55500007 + 4.3 * Math.Sin(Math.PI / 10);
					var rx = x * Math.Cos(angle) - y * Math.Sin(angle);
					var ry = x * Math.Sin(angle) + y * Math.Cos(angle);
					var world = body.LocalToWorld(turret.Offset + arm.LocalOffset[b].Rotate(WRot.FromYaw(new WAngle(i * 32))));
					var dx = world.X * (double)grid.TileSize.Width / grid.TileScale - .6 * (-13 + 20 * (rx + ry) / Math.Sqrt(2));
					var dy = (world.Y - world.Z) * (double)grid.TileSize.Height / grid.TileScale - (12.6 + .6 * (-66 + 20 * (rx - ry) / Math.Sqrt(8) - 20 * z * Math.Sqrt(3) / 2));
					error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
				}
			if (error > .6) throw new InvalidDataException("SAM muzzle registration: " + error);
			Console.WriteLine($"Nod SAM: actual grid {grid.TileSize.Width}x{grid.TileSize.Height}, 60% size, centered bearing and fitted selection; all five 32-facing states loaded, preview full, 4/10/30 cadence, whole-magazine reload; 128 muzzle projections max error {error:F3}px. Live firing review pending.");
		}
	}
}
