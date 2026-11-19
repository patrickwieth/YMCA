using System;
using System.IO;
using System.Security.Cryptography;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Explicit diagnostic only. All files/processes belong to a fresh temp profile.
	static class TournamentMapDownloadProbe
	{
		public static void Start(Widget widget, ModData mod, OrderManager manager)
		{
			var requested = Environment.GetEnvironmentVariable("YMCA_MAP_DOWNLOAD_PROBE");
			if (string.IsNullOrEmpty(requested)) return;
			var output = Path.GetFullPath(requested);
			if (!output.StartsWith(Path.Combine(Path.GetTempPath(), "ymca-map-download-probes") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("Use the download probe temp directory.");
			Directory.CreateDirectory(output);
			var role = Environment.GetEnvironmentVariable("YMCA_MAP_DOWNLOAD_PROBE_ROLE");
			var log = Path.Combine(output, "results.txt");
			File.WriteAllText(log, "START " + role + "\n");
			var expectedUid = Environment.GetEnvironmentVariable("YMCA_MAP_DOWNLOAD_PROBE_UID") ?? throw new ArgumentException("Probe map UID required.");
			var initiallyAvailable = mod.MapCache[expectedUid].Status == MapStatus.Available;
			void Check(bool condition, string text)
			{
				if (!condition) throw new InvalidOperationException("Map download probe: " + text);
				File.AppendAllText(log, "PASS " + text + "\n");
			}
			void Poll(Func<bool> condition, Action work, int attempts = 800)
			{
				Game.RunAfterDelay(250, () =>
				{
					if (condition()) work();
					else if (attempts > 0) Poll(condition, work, attempts - 1);
					else
					{
						Game.Renderer.SaveScreenshot(Path.Combine(output, "timeout.png"));
						throw new InvalidOperationException("Download probe timeout: " + role + " started=" + manager.GameStarted + " tick=" + manager.World?.WorldTick);
					}
				});
			}
			bool HasResult(string other, string text)
			{
				var path = Path.Combine(Directory.GetParent(output).FullName, other, "results.txt");
				return File.Exists(path) && File.ReadAllText(path).Contains(text);
			}
			Poll(() => TournamentMapDownloadLogic.HasDownload(manager.LobbyInfo.GlobalSettings), () =>
			{
				var settings = manager.LobbyInfo.GlobalSettings;
				TournamentMapDownloadInfo.TryParse(settings.LobbyOptions[TournamentMapDownloadInfo.LobbyOption].Value, out var info);
				Check(info.Uid == expectedUid && !initiallyAvailable, "map was not installed before joining");
				Check(settings.OptionOrDefault(GeneratedMapRecipe.Capability, "") == "map-pool", "fixed tournament pool retained; not recipe generation");
				if (role == "optin")
				{
					Check(!Game.Settings.Game.AllowDownloading, "automatic downloads disabled by preference");
					Game.RunAfterDelay(1000, () =>
					{
						Check(mod.MapCache[info.Uid].Status != MapStatus.Available && manager.LocalClient.State == Session.ClientState.Invalid,
							"no unsolicited download or ready state");
						widget.Get<ButtonWidget>("DOWNLOAD_RETRY").OnClick();
					});
				}
				Poll(() => mod.MapCache[info.Uid].Status == MapStatus.Available && manager.LocalClient.State == Session.ClientState.NotReady, () =>
				{
					using var stream = File.OpenRead(mod.MapCache[info.Uid].PackageName);
					using var sha = SHA256.Create();
					Check(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() == info.Sha256, "downloaded exact server package");
					Check(!widget.Get("TOURNAMENT_MAP_DOWNLOAD").IsVisible(), "progress overlay removed after hot-loading");
					if (role == "spectator") Check(manager.LocalClient.IsObserver, "spectator downloaded the map too");
					File.AppendAllText(log, "MAP_READY " + info.Uid + "\n");
					Game.Renderer.SaveScreenshot(Path.Combine(output, "ready.png"));
					if (role != "host" && role != "optin") return;
					Poll(() => HasResult("optin", "MAP_READY"), () => manager.IssueOrder(Order.Command("state Ready")));
					Poll(() => manager.GameStarted && manager.World != null && manager.World.WorldTick >= 100, () =>
					{
						File.AppendAllText(log, "MATCH_STARTED\n");
						if (Environment.GetEnvironmentVariable("YMCA_POSTGAME_PROBE") != "1") return;
						if (role == "host") manager.World.IssueOrder(new Order("Surrender", manager.World.LocalPlayer.PlayerActor, false));
						Poll(() => manager.World.IsGameOver, () =>
						{
							File.AppendAllText(log, "GAME_OVER " + DateTime.UtcNow.ToString("O") + "\n");
							Game.RunAfterDelay(170000, () =>
							{
								Check(manager.Connection is NetworkConnection connection && connection.ConnectionState == ConnectionState.Connected,
									"still connected 170 seconds after game over");
								Game.Renderer.SaveScreenshot(Path.Combine(output, "postgame.png"));
								File.AppendAllText(log, "STATS_STILL_CONNECTED\n");
							});
						});
					});
				});
			});
		}
	}
}
