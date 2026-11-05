using System;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Runs after LobbyLogic, retaining its visibility, readiness and start guards.
	public class GeneratedMapLobbyLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public GeneratedMapLobbyLogic(Widget widget, ModData modData, OrderManager orderManager, bool skirmishMode)
		{
			if (!skirmishMode) return;
			var button = widget.GetOrNull<ButtonWidget>("CHANGEMAP_BUTTON");
			if (button == null) return;
			var chooseExisting = button.OnClick;
			button.OnClick = () =>
			{
				if (!Game.IsHost) { chooseExisting(); return; }
				modData.MapCache.UpdateMaps();
				Game.OpenWindow("SKIRMISH_SETUP", new WidgetArgs
				{
					{ "onSelect", (Action<string>)(uid =>
					{
						if (string.IsNullOrEmpty(uid) || !Game.IsHost || button.IsDisabled() || uid == orderManager.LobbyInfo.GlobalSettings.Map)
							return;
						var status = modData.MapCache[uid].Status;
						if (status != MapStatus.Available && status != MapStatus.DownloadAvailable) return;
						// Change the existing server's map, rather than starting another lobby.
						orderManager.IssueOrder(Order.Command("map " + uid));
						Game.Settings.Server.Map = uid;
						Game.Settings.Save();
					}) },
				});
			};
		}
	}
}
