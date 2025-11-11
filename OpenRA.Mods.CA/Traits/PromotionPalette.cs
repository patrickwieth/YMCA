using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Controls Commander tree layout hints such as fixed columns and groupings.")]
	public class PromotionPaletteInfo : TraitInfo<PromotionPalette>
	{
		[Desc("Zero-based column index for the promotion icon. Leave negative to use automatic layout.")]
		public readonly int Column = -1;

		[Desc("Group identifier used to draw a shared frame around related promotions.")]
		public readonly string Group;
	}

	public sealed class PromotionPalette
	{
	}
}
