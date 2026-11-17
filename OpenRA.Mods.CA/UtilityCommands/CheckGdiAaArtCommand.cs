using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-gdi-aa-art", "Validate approved native CRAM art, separation and muzzle registration.")]
	public sealed class CheckGdiAaArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-gdi-aa-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["cram"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "cram.native" ||
				actor.TraitInfo<WithSpriteBodyInfo>().Sequence != "base" ||
				!actor.HasTraitInfo<WithSpriteTurretInfo>() || actor.HasTraitInfo<WithEmbeddedTurretSpriteBodyInfo>())
				throw new InvalidDataException("CRAM must use its approved separate native base/turret.");
			foreach (var item in new[] { ("aa-gun-base.png", 1), ("aa-gun-turret32.png", 32) })
			{
				var path = "bits/gdi/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 192 || f.Size.Height != 192 ||
					f.FrameSize != f.Size || f.Offset != float2.Zero || f.Data.Length != 192 * 192 * 4))
					throw new InvalidDataException(path + ": invalid native frames.");
			}
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var arms = actor.TraitInfos<ArmamentInfo>().ToArray();
			if (arms.Length != 4 || turret.TurnSpeed.Angle != 60 || turret.InitialFacing.Angle != 224)
				throw new InvalidDataException("CRAM firing stages or turning settings changed.");
			var grid = mod.Manifest.Get<MapGrid>();
			if (body.UseClassicPerspectiveFudge) throw new InvalidDataException("CRAM requires actual-grid projection.");
			var pixelsX = (double)grid.TileSize.Width / grid.TileScale;
			var pixelsY = (double)grid.TileSize.Height / grid.TileScale;
			var offset = body.LocalToWorld(turret.Offset);
			var radial = .75 + 2.35 * Math.Cos(25 * Math.PI / 180);
			var z = 3.12 + 2.35 * Math.Sin(25 * Math.PI / 180);
			double maxError = 0;
			foreach (var arm in arms)
				for (var i = 0; i < 32; i++)
				{
					var muzzle = offset + body.LocalToWorld(arm.LocalOffset[0].Rotate(WRot.FromYaw(new WAngle(i * 32))));
					var angle = (4 + i) * Math.PI / 16;
					var x = -radial * Math.Sin(angle);
					var y = radial * Math.Cos(angle);
					var dx = muzzle.X * pixelsX - 20 * (x + y) / Math.Sqrt(2);
					var dy = (muzzle.Y - muzzle.Z) * pixelsY - (20 * (x - y) / Math.Sqrt(8) - 20 * z * Math.Sqrt(3) / 2);
					maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
				}
			if (maxError > 1)
				throw new InvalidDataException($"CRAM muzzle projection error: {maxError:F3}px.");
			Console.WriteLine($"CRAM: native 1+32 frames, separate traits and muzzle registration passed ({maxError:F3}px). Live firing review pending.");
		}
	}
}
