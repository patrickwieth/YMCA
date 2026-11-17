using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Voxel barrel with a configurable recoil vector for pre-elevated models.")]
	public class WithVoxelBarrelAxisInfo : WithVoxelBarrelInfo
	{
		[Desc("Local displacement per 1024 recoil units. Includes the mod's vertical projection conversion.")]
		public readonly WVec RecoilDirection = new(-1024, 0, 0);

		public override object Create(ActorInitializer init) { return new WithVoxelBarrelAxis(init.Self, this); }
	}

	public class WithVoxelBarrelAxis : ConditionalTrait<WithVoxelBarrelAxisInfo>
	{
		public static WVec RecoilOffset(WVec direction, WDist recoil)
		{
			return new WVec(direction.X * recoil.Length / 1024,
				direction.Y * recoil.Length / 1024, direction.Z * recoil.Length / 1024);
		}

		public WithVoxelBarrelAxis(Actor self, WithVoxelBarrelAxisInfo info)
			: base(info)
		{
			var body = self.Trait<BodyOrientation>();
			var armament = self.TraitsImplementing<Armament>().First(a => a.Info.Name == info.Armament);
			var turret = self.TraitsImplementing<Turreted>().First(t => t.Name == armament.Info.Turret);
			var renderer = self.Trait<RenderVoxels>();
			WVec Offset()
			{
				var local = info.LocalOffset + RecoilOffset(info.RecoilDirection, armament.Recoil);
				return body.LocalToWorld(local.Rotate(turret.WorldOrientation)
					+ turret.Offset.Rotate(body.QuantizeOrientation(self.Orientation)));
			}

			renderer.Add(new ModelAnimation(renderer.Renderer.ModelCache.GetModelSequence(renderer.Image, info.Sequence),
				Offset, () => info.LocalOrientation.Rotate(turret.WorldOrientation),
				() => IsTraitDisabled, () => 0, info.ShowShadow));
		}
	}
}
