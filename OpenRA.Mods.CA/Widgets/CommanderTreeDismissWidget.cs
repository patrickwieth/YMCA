using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class CommanderTreeDismissWidget : ColorBlockWidget
	{
		[ObjectCreator.UseCtor]
		public CommanderTreeDismissWidget(ModData modData)
			: base(modData)
		{
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				TakeMouseFocus(mi);
				OnMouseDown(mi);
				return true;
			}

			if (mi.Event == MouseInputEvent.Up)
			{
				OnMouseUp(mi);
				YieldMouseFocus(mi);
				return true;
			}

			return true;
		}
	}
}
