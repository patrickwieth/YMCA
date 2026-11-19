using System;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public sealed class TournamentMapDownloadLogic : ChromeLogic
	{
		readonly ModData mod;
		readonly OrderManager manager;
		CancellationTokenSource pending;
		string selection;
		string status = "";
		bool disposed, busy, attempted;
		volatile int generation;
		volatile int progress;

		public static bool HasDownload(Session.Global settings) => TryGetDownload(settings, out _);

		static bool TryGetDownload(Session.Global settings, out TournamentMapDownloadInfo info)
		{
			info = null;
			return settings.LobbyOptions.TryGetValue(TournamentMapDownloadInfo.LobbyOption, out var value) &&
				TournamentMapDownloadInfo.TryParse(value.Value, out info) && info.Uid == settings.Map;
		}

		[ObjectCreator.UseCtor]
		public TournamentMapDownloadLogic(Widget widget, ModData modData, OrderManager orderManager)
		{
			mod = modData; manager = orderManager;
			var repositoryRetry = widget.GetOrNull<ButtonWidget>("MAP_RETRY");
			if (repositoryRetry != null)
			{
				var originalRetry = repositoryRetry.OnClick;
				repositoryRetry.OnClick = () =>
				{
					var settings = manager.LobbyInfo.GlobalSettings;
					if (!HasDownload(settings) && !GeneratedMapLobbyLogic.HasGeneratedMapRecipe(settings)) originalRetry();
				};
			}
			var panel = widget.Get("TOURNAMENT_MAP_DOWNLOAD");
			panel.IsVisible = () => !disposed && HasDownload(manager.LobbyInfo.GlobalSettings) &&
				mod.MapCache[manager.LobbyInfo.GlobalSettings.Map].Status != MapStatus.Available;
			panel.Get<LabelWidget>("DOWNLOAD_STATUS").GetText = () => busy ? "Downloading: " + progress + "%" : status;
			var retry = panel.Get<ButtonWidget>("DOWNLOAD_RETRY");
			retry.GetText = () => attempted ? "Retry download" : "Download map";
			retry.IsVisible = () => !busy;
			retry.OnClick = () => Start(true);
			Game.LobbyInfoChanged += Synchronize;
			Synchronize();
			TournamentMapDownloadProbe.Start(widget, modData, orderManager);
		}

		void Synchronize()
		{
			if (disposed) return;
			var current = TryGetDownload(manager.LobbyInfo.GlobalSettings, out var info) ? info.Encode() : null;
			if (current != selection)
			{
				generation++;
				pending?.Cancel();
				selection = current;
				busy = attempted = false;
				status = "Map required";
			}
			Start(false);
		}

		void Start(bool explicitRequest)
		{
			if (disposed || busy || (!explicitRequest && (attempted || !Game.Settings.Game.AllowDownloading)) ||
				!TryGetDownload(manager.LobbyInfo.GlobalSettings, out var info) || mod.MapCache[info.Uid].Status == MapStatus.Available) return;
			attempted = busy = true; progress = 0;
			var request = ++generation;
			var cancellation = new CancellationTokenSource();
			pending = cancellation;
			TextNotificationsManager.AddSystemLine("Tournament map", "Downloading the server's map...");
			_ = Task.Run(async () =>
			{
				byte[] bytes = null; Exception error = null;
				try
				{
					bytes = await TournamentMapTransfer.DownloadAsync(info, cancellation.Token, value => { if (request == generation) progress = value; });
					ReplayMapProvider.ValidateArchive(bytes, info.Uid);
				}
				catch (Exception e) { error = e; }
				Game.RunAfterTick(() =>
				{
					var cancelled = cancellation.IsCancellationRequested;
					if (pending == cancellation) pending = null;
					cancellation.Dispose();
					if (disposed || cancelled || request != generation || !TryGetDownload(manager.LobbyInfo.GlobalSettings, out var current) || current.Encode() != info.Encode()) return;
					busy = false;
					try
					{
						if (error != null) throw error;
						GeneratedMapInstallation.InstallArchive(mod, bytes, info.Uid);
						manager.IssueOrder(Order.Command("state " + Session.ClientState.NotReady));
						status = "Map ready";
						TextNotificationsManager.AddSystemLine("Tournament map", "Map downloaded and verified. You can now ready up.");
					}
					catch (Exception e)
					{
						status = "Download failed";
						Log.Write("debug", e);
						TextNotificationsManager.AddSystemLine("Tournament map", "Could not download map: " + e.Message + " Use Retry download to try again.");
					}
				});
			});
		}

		protected override void Dispose(bool disposing)
		{
			disposed = true; generation++;
			pending?.Cancel();
			if (disposing) Game.LobbyInfoChanged -= Synchronize;
			base.Dispose(disposing);
		}
	}
}
