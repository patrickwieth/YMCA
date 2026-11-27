using System;
using System.Collections.Generic;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Game.IsHost is the lobby administrator, including on a remote dedicated server.
	public class GeneratedMapLobbyLogic : ChromeLogic
	{
		readonly ModData modData;
		readonly OrderManager orderManager;
		readonly HashSet<string> attempted = new();
		bool disposed;

		public static bool HasGeneratedMapRecipe(Session.Global settings) =>
			settings.LobbyOptions.TryGetValue(GeneratedMapRecipe.Capability, out var capability) && capability.Value == GeneratedMapRecipe.Version &&
			settings.LobbyOptions.TryGetValue(GeneratedMapRecipe.Selection, out var selection) &&
			selection.Value != null && selection.Value.Length <= 105 && selection.Value.StartsWith(settings.Map + ":", StringComparison.Ordinal);

		[ObjectCreator.UseCtor]
		public GeneratedMapLobbyLogic(Widget widget, ModData modData, OrderManager orderManager, bool skirmishMode)
		{
			this.modData = modData;
			this.orderManager = orderManager;
			Game.LobbyInfoChanged += SynchronizeGeneratedMap;
			SynchronizeGeneratedMap();
			var button = widget.GetOrNull<ButtonWidget>("CHANGEMAP_BUTTON");
			if (button == null) return;
			var chooseExisting = button.OnClick;
			bool CanChange() => !disposed && orderManager.LocalClient?.IsAdmin == true && !button.IsDisabled();
			button.OnClick = () =>
			{
				if (!CanChange()) { chooseExisting(); return; }
				modData.MapCache.UpdateMaps();
				var originalMap = orderManager.LobbyInfo.GlobalSettings.Map;
				var args = new WidgetArgs
				{
					{ "allowedMapPool", orderManager.ServerMapPool },
					{ "onSelect", (Action<string>)(uid =>
					{
						if (string.IsNullOrEmpty(uid) || !CanChange() || uid == orderManager.LobbyInfo.GlobalSettings.Map) return;
						var status = modData.MapCache[uid].Status;
						if (status != MapStatus.Available && status != MapStatus.DownloadAvailable) return;
						orderManager.IssueOrder(Order.Command("map " + uid));
						Game.Settings.Server.Map = uid;
						Game.Settings.Save();
					}) },
				};
				if (!skirmishMode)
				{
					args.Add("onChooseExisting", (Action)(() => { if (!disposed) chooseExisting(); }));
					args.Add("onGenerateRecipe", (Action<string>)(recipe =>
					{
						if (!CanChange() || originalMap != orderManager.LobbyInfo.GlobalSettings.Map)
							throw new InvalidOperationException("Lobby permissions or the selected map changed. Reopen Change Map.");
						orderManager.LobbyInfo.GlobalSettings.LobbyOptions.TryGetValue(GeneratedMapRecipe.Capability, out var capability);
						if (capability?.Value == "map-pool")
							throw new InvalidOperationException("This server restricts map selection to its configured map pool.");
						if (capability?.Value != GeneratedMapRecipe.Version)
							throw new InvalidOperationException("This server needs an update to support random map generation.");
						orderManager.IssueOrder(Order.Command(GeneratedMapRecipe.Command + originalMap + " " + recipe));
					}));
				}

				SkirmishSetupLogic.Open(args);
			};
			DedicatedMapGeneratorProbe.Start(widget, modData, orderManager);
			LanMapGeneratorProbe.StartLobby(widget, modData, orderManager);
		}

		void SynchronizeGeneratedMap()
		{
			var settings = orderManager.LobbyInfo.GlobalSettings;
			if (disposed || TournamentMapDownloadLogic.HasDownload(settings) || !HasGeneratedMapRecipe(settings)) return;
			var uid = settings.Map;
			if (modData.MapCache[uid].Status == MapStatus.Available || !attempted.Add(uid)) return;
			var recipe = settings.LobbyOptions[GeneratedMapRecipe.Selection].Value.Substring(uid.Length + 1);
			TextNotificationsManager.AddSystemLine("Map generator", "Preparing the server's generated map...");
			Game.RunAfterDelay(100, () =>
			{
				if (disposed || orderManager.LobbyInfo.GlobalSettings.Map != uid)
				{
					attempted.Remove(uid);
					return;
				}

				try
				{
					if (modData.MapCache[uid].Status != MapStatus.Available)
					{
						var options = GeneratedMapRecipe.Decode(recipe);
						var actual = GeneratedMapInstallation.Install(modData, MapPlanGenerator.Generate(options), options);
						if (actual != uid) throw new InvalidOperationException("Generated map checksum differs from the server. Client/server generator versions must match.");
					}

					orderManager.IssueOrder(Order.Command("state " + Session.ClientState.NotReady));
					TextNotificationsManager.AddSystemLine("Map generator", "Generated map is ready.");
				}
				catch (Exception e)
				{
					Log.Write("debug", e);
					TextNotificationsManager.AddSystemLine("Map generator", "Could not prepare map: " + e.Message);
				}
			});
		}

		protected override void Dispose(bool disposing)
		{
			disposed = true;
			if (disposing) Game.LobbyInfoChanged -= SynchronizeGeneratedMap;
			base.Dispose(disposing);
		}
	}
}
