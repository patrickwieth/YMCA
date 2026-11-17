using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.Cnc.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckScrinShardLauncherArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-scrin-shard-launcher-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Validate SHAR native parts,1x1 occupancy, unchanged AA weapon and32 muzzle projections.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["shar"];
			_ = actor.TraitsInConstructOrder().ToArray();
			var building = actor.TraitInfo<BuildingInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var arm = actor.TraitInfos<ArmamentInfo>().Single();
			if (building.Dimensions != new CVec(1, 1) || actor.HasTraitInfo<WithBuildingBibInfo>() ||
				actor.TraitInfo<RenderSpritesInfo>().Image != "shar.native" || actor.TraitInfo<WithSpriteBodyInfo>().Sequence != "base" ||
				!actor.HasTraitInfo<WithSpriteTurretInfo>() || body.UseClassicPerspectiveFudge)
				throw new InvalidDataException("Shard Launcher native1x1 attachment/binding regression.");
			if (arm.Weapon != "ShardLauncher" || turret.TurnSpeed.Angle != 60 || turret.InitialFacing.Angle != 48)
				throw new InvalidDataException("Shard Launcher weapon/turning changed.");
			var weapon = mod.DefaultRules.Weapons[arm.Weapon.ToLowerInvariant()];
			if (weapon.Burst != 5 || weapon.ReloadDelay != 25 || !weapon.BurstDelays.SequenceEqual(new[] { 4 }) ||
				!weapon.ValidTargets.Equals(new BitSet<TargetableType>("Air")))
				throw new InvalidDataException("Shard Launcher must retain its five-shot AA-only weapon.");
			foreach (var item in new[] { ("shard-launcher-base.png", 1), ("shard-launcher-turret32.png", 32) })
			{
				var path = "bits/scrin/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 384 || f.Size.Height != 384 || f.Offset != float2.Zero))
					throw new InvalidDataException("Incorrect native PNG frames: " + path);
			}
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == "shar.native");
			var names = new[] { "Defaults", "idle", "damaged-idle", "base", "damaged-base", "turret", "damaged-turret", "construction", "icon" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			if (sequences["turret"].Facings != 32 || sequences["damaged-turret"].Facings != 32 ||
				sequences["base"].Length != 1 || Math.Abs(sequences["base"].Scale - .58f) > .0001f ||
				Math.Abs(sequences["turret"].Scale - .58f) > .0001f || sequences["construction"].Length != 10)
				throw new InvalidDataException("Shard Launcher sequence layout/scale/construction regression.");
			var grid = mod.Manifest.Get<MapGrid>();
			var baseOffset = sequences["base"].GetSprite(0).Offset.Y;
			var turretOffset = sequences["turret"].GetSprite(0).Offset.Y;
			if (baseOffset != -88 || Math.Abs(turretOffset + 59.423128) > .0001 ||
				Math.Abs(sequences["damaged-turret"].GetSprite(0).Offset.Y - turretOffset) > .0001)
				throw new InvalidDataException("Shard Launcher resolved sprite offsets changed.");
			var residual = (turretOffset - baseOffset) * .58 - turret.Offset.Z * (double)grid.TileSize.Height / grid.TileScale;
			if (turret.Offset != new WVec(0, 0, 375) || arm.LocalOffset.Single() != new WVec(577, -35, 1490))
				throw new InvalidDataException("Shard Launcher pass30 attachment registration changed.");
			double error = 0;
			for (var i = 0; i < 32; i++)
			{
				var p = body.LocalToWorld(turret.Offset + arm.LocalOffset.Single().Rotate(WRot.FromYaw(new WAngle(i * 32))));
				var angle = (4 + i) * Math.PI / 16;
				var x = -.192 * Math.Cos(angle) - 3.141533851623535 * Math.Sin(angle);
				var y = -.192 * Math.Sin(angle) + 3.141533851623535 * Math.Cos(angle);
				var dx = p.X * (double)grid.TileSize.Width / grid.TileScale - 28 * .58 * (x + y) / Math.Sqrt(2);
				var dy = (p.Y - p.Z) * (double)grid.TileSize.Height / grid.TileScale -
					(28 * .58 * ((x - y) / Math.Sqrt(8) - 5.862306118011475 * Math.Sqrt(3) / 2) + residual);
				error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
			}
			if (error > .4) throw new InvalidDataException("Shard Launcher muzzle registration: " + error);
			Console.WriteLine($"SHAR: fixed base + native32 turret loaded,1x1, existing five-shot AA weapon/construction/icon retained; actual-grid muzzle max{error:F3}px. Live firing review pending.");
		}
	}
}
