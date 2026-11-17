using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckNodLaserArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-nod-laser-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Validate approved Nod laser parts, dependencies and muzzle projection.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["ltur"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "ltur.native" || !actor.HasTraitInfo<WithSpriteTurretInfo>())
				throw new InvalidDataException("Nod laser parts not bound.");
			foreach (var item in new[] { ("laser-base.png", 1), ("laser-turret32.png", 32) })
			{
				var path = "bits/nod/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 256 || f.Size.Height != 256))
					throw new InvalidDataException("Incorrect native Nod laser frames.");
			}
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var grid = mod.Manifest.Get<MapGrid>();
			if (body.UseClassicPerspectiveFudge) throw new InvalidDataException("Nod laser requires actual-grid projection.");
			double maxError = 0;
			foreach (var arm in actor.TraitInfos<ArmamentInfo>())
			{
				if (arm.Weapon != "LaserTur" && arm.Weapon != "LaserTur.Adv")
					throw new InvalidDataException("Unexpected Nod laser weapon.");
				for (var i = 0; i < 32; i++)
				{
					var a = (20 + i) * Math.PI / 16;
					var x = 4.21 * Math.Sin(a); var y = -4.21 * Math.Cos(a);
					var muzzle = body.LocalToWorld(turret.Offset + arm.LocalOffset[0].Rotate(WRot.FromYaw(new WAngle(i * 32))));
					var dx = muzzle.X * (double)grid.TileSize.Width / grid.TileScale - 10 * (x + y) / Math.Sqrt(2);
					var dy = (muzzle.Y - muzzle.Z) * (double)grid.TileSize.Height / grid.TileScale - (10 * (x - y) / Math.Sqrt(8) - 10 * 4.4 * Math.Sqrt(3) / 2);
					maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
				}
			}
			if (maxError > .5) throw new InvalidDataException("Muzzle registration: " + maxError);
			Console.WriteLine($"Nod laser original native parts/dependencies loaded at 50% registration; max muzzle error {maxError:F3}px. Live firing review pending.");
		}
	}
}
