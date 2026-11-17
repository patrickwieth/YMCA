using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows a wider local camera view without changing starting zoom, maximum zoom-in or game visibility.")]
	public class ExtendedWorldZoomInfo : TraitInfo
	{
		[Desc("Minimum camera scale relative to the normal zoom-out limit. One third shows three times the width and height.")]
		public readonly float MinimumZoomScale = 1f / 3f;

		public override object Create(ActorInitializer init) => new ExtendedWorldZoom(this);
	}

	public class ExtendedWorldZoom : IPostWorldLoaded, ITickRender, INotifyViewportZoomExtentsChanged
	{
		readonly ExtendedWorldZoomInfo info;
		float effectiveMinimum = float.MaxValue;

		public ExtendedWorldZoom(ExtendedWorldZoomInfo info)
		{
			if (!(info.MinimumZoomScale > 0 && info.MinimumZoomScale < 1))
				throw new System.ArgumentOutOfRangeException(nameof(info.MinimumZoomScale));
			this.info = info;
		}

		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr) => Apply(wr);

		void INotifyViewportZoomExtentsChanged.ViewportZoomExtentsChanged(float minZoom, float maxZoom)
		{
			effectiveMinimum = minZoom;
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			// The observer UI can reset the limit after world loading. Restore our
			// wider range once, not every frame. Keep any still-wider diagnostic range.
			if (effectiveMinimum > wr.Viewport.MinZoom * info.MinimumZoomScale + 0.000001f)
				Apply(wr);
		}

		void Apply(WorldRenderer wr)
		{
			wr.Viewport.UnlockMinimumZoom(info.MinimumZoomScale);
		}
	}
}
