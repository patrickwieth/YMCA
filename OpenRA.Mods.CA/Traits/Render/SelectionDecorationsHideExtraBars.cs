using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Selection decorations that can suppress extra status bars when the actor is not selected.")]
	public class SelectionDecorationsHideExtraBarsInfo : SelectionDecorationsInfo
	{
		[Desc("If true, the extra (non-health) selection bars are only shown while this actor is selected.")]
		public readonly bool ExtraBarsRequireSelection = true;

		public override object Create(ActorInitializer init) { return new SelectionDecorationsHideExtraBars(init.Self, this); }
	}

	public class SelectionDecorationsHideExtraBars : SelectionDecorations
	{
		readonly SelectionDecorationsHideExtraBarsInfo info;

		public SelectionDecorationsHideExtraBars(Actor self, SelectionDecorationsHideExtraBarsInfo info)
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
