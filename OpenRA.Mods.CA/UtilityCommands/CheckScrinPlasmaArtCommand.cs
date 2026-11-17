using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckScrinPlasmaArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-scrin-plasma-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Load native plasma turret parts and validate1x1, ground targeting and64 muzzle projections.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["ptur"];
			_ = actor.TraitsInConstructOrder().ToArray();
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var arm = actor.TraitInfos<ArmamentInfo>().Single();
			if (actor.TraitInfo<BuildingInfo>().Dimensions != new CVec(1, 1) || actor.HasTraitInfo<WithBuildingBibInfo>() ||
				actor.TraitInfo<RenderSpritesInfo>().Image != "ptur.native" || actor.TraitInfo<WithSpriteBodyInfo>().Sequence != "base" ||
				body.UseClassicPerspectiveFudge || turret.TurnSpeed.Angle != 48 || arm.LocalOffset.Length != 2 || arm.Weapon != "PlasmaTurretGun")
				throw new InvalidDataException("PTUR native attachment/gameplay regression.");
			var weapon = mod.DefaultRules.Weapons[arm.Weapon.ToLowerInvariant()];
			if (weapon.Burst != 2 || weapon.ReloadDelay != 15 || !weapon.BurstDelays.SequenceEqual(new[] { 4 }) ||
				!weapon.ValidTargets.Equals(new BitSet<TargetableType>("Ground", "Water")))
				throw new InvalidDataException("PTUR must retain its ground/surface-only two-shot weapon.");
			foreach (var item in new[] { ("plasma-cannon-base.png", 1), ("plasma-cannon-turret32.png", 32) })
			{
				var path = "bits/scrin/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 320 || f.Size.Height != 320 || f.Type == SpriteFrameType.Indexed8))
					throw new InvalidDataException(path + ": native RGBA frame layout regression.");
			}
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == "ptur.native");
			var names = new[] { "Defaults", "idle", "damaged-idle", "base", "damaged-base", "turret", "damaged-turret", "muzzle", "icon", "construction" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			if (sequences["turret"].Facings != 32 || sequences["damaged-turret"].Facings != 32 ||
				sequences["base"].Length != 1 || Math.Abs(sequences["base"].Scale - .58f) > .0001f ||
				sequences["construction"].Length != 10 || sequences["muzzle"].Length != 4 || sequences["muzzle"].Tick != 80)
				throw new InvalidDataException("PTUR native/damaged/construction/flash sequence regression.");
			var grid = mod.Manifest.Get<MapGrid>();
			var residual = 52 * .58 - turret.Offset.Z * (double)grid.TileSize.Height / grid.TileScale;
			double error = 0;
			for (var i = 0; i < 32; i++)
				for (var j = 0; j < 2; j++)
				{
					var point = body.LocalToWorld(turret.Offset + arm.LocalOffset[j].Rotate(WRot.FromYaw(new WAngle(i * 32))));
					var angle = (4 + i) * Math.PI / 16;
					var lateral = j == 0 ? -.12 : .12;
					var x = lateral * Math.Cos(angle) - 1.9725 * Math.Sin(angle);
					var y = lateral * Math.Sin(angle) + 1.9725 * Math.Cos(angle);
					var dx = point.X * (double)grid.TileSize.Width / grid.TileScale - 28 * .58 * (x + y) / Math.Sqrt(2);
					var dy = (point.Y - point.Z) * (double)grid.TileSize.Height / grid.TileScale -
						(28 * .58 * ((x - y) / Math.Sqrt(8) - 4.42 * Math.Sqrt(3) / 2) + residual);
					error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
				}
			if (error > .4) throw new InvalidDataException("PTUR muzzle registration: " + error);
			Console.WriteLine($"PTUR native32/base/damaged/construction/flash loaded;1x1 ground/surface targeting, no AA;64 muzzle projections max{error:F3}px. Live firing review pending.");
		}
	}
}
