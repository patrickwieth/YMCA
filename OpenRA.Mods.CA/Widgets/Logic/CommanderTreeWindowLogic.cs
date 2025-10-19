using System;
using System.Linq;
using OpenRA;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class CommanderTreeWindowLogic : ChromeLogic
	{
		readonly World world;
		readonly LabelWidget pointsValue;
		readonly ButtonWidget closeButton;
		readonly ColorBlockWidget dismissArea;

		[ObjectCreator.UseCtor]
		public CommanderTreeWindowLogic(Widget widget, World world)
		{
			this.world = world;

			pointsValue = widget.GetOrNull<LabelWidget>("POINTS_VALUE");
			closeButton = widget.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			dismissArea = widget.GetOrNull<ColorBlockWidget>("DISMISS_AREA");

			if (closeButton != null)
				closeButton.OnClick = Close;

			if (dismissArea != null)
			{
				dismissArea.OnMouseDown = _ => { };
				dismissArea.OnMouseUp = _ => Close();
			}

			if (pointsValue != null)
				pointsValue.GetText = () => GetCommanderPoints().ToStringInvariant();
		}

		int GetCommanderPoints()
		{
			var player = world.LocalPlayer;
			if (player == null)
				return 0;

			var promotionsTrait = player.PlayerActor
				.TraitsImplementing<ISync>()
				.FirstOrDefault(t => string.Equals(t.GetType().Name, "PlayerPromotions", StringComparison.OrdinalIgnoreCase));

			if (promotionsTrait != null)
			{
				var pointsField = promotionsTrait.GetType().GetField("Points");
				if (pointsField != null && pointsField.FieldType == typeof(int))
					return (int)pointsField.GetValue(promotionsTrait);
			}

			return 0;
		}

		void Close()
		{
			Ui.CloseWindow();
		}
	}
}

