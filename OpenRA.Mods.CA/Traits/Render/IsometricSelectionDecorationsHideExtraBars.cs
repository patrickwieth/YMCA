using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Isometric selection decorations that can suppress extra status bars when the actor is not selected.")]
	public class IsometricSelectionDecorationsHideExtraBarsInfo : IsometricSelectionDecorationsInfo
	{
		[Desc("If true, the extra (non-health) selection bars are only shown while this actor is selected.")]
		public readonly bool ExtraBarsRequireSelection = true;

		public override object Create(ActorInitializer init) { return new IsometricSelectionDecorationsHideExtraBars(init.Self, this); }
	}

	public class IsometricSelectionDecorationsHideExtraBars : IsometricSelectionDecorations
	{
		readonly IsometricSelectionDecorationsHideExtraBarsInfo info;

		public IsometricSelectionDecorationsHideExtraBars(Actor self, IsometricSelectionDecorationsHideExtraBarsInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra)
		{
			if (info.ExtraBarsRequireSelection && !self.World.Selection.Contains(self))
				displayExtra = false;

			return base.RenderSelectionBars(self, wr, displayHealth, displayExtra);
		}
	}
}
