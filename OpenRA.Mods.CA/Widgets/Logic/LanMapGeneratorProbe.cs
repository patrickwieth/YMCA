using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Explicit loopback diagnostic only; never active in normal clients.
	static class LanMapGeneratorProbe
	{
		static string Output()
		{
			var value = Environment.GetEnvironmentVariable("YMCA_LAN_MAP_PROBE");
			if (string.IsNullOrEmpty(value)) return null;
			var path = Path.GetFullPath(value);
			var root = Path.Combine(Path.GetTempPath(), "ymca-lan-map-probes") + Path.DirectorySeparatorChar;
			if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("LAN probe output must be in its temp directory.");
			Directory.CreateDirectory(path);
			return path;
		}

		static void Poll(Func<bool> condition, Action action, int attempts = 500)
		{
			Game.RunAfterDelay(500, () =>
			{
				if (condition()) action();
				else if (attempts > 0) Poll(condition, action, attempts - 1);
				else throw new InvalidOperationException("LAN map probe timed out.");
			});
		}

		static void Click(string text) => Ui.CurrentWindow().Get("CONTENT").Children.OfType<ButtonWidget>().Single(b => b.GetText() == text).OnClick();

		public static void StartMenu(Widget menu)
		{
			if (Environment.GetEnvironmentVariable("YMCA_LAN_MAP_PROBE_ROLE") != "host" || Output() == null) return;
			Action loaded = null;
			loaded = () =>
			{
				Game.OnShellmapLoaded -= loaded;
				Game.RunAfterDelay(1200, () =>
				{
					menu.Get<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick();
					Ui.CurrentWindow().Get<ButtonWidget>("CREATE_BUTTON").OnClick();
					var panel = Ui.CurrentWindow();
					panel.Get<TextFieldWidget>("LISTEN_PORT").Text = "57351";
					panel.Get<TextFieldWidget>("SERVER_NAME").Text = "LocalLANMapTest";
					var advertise = panel.Get<CheckboxWidget>("ADVERTISE_CHECKBOX");
					if (advertise.IsChecked()) advertise.OnClick();
					panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick();
					Click("Tactical"); Click("Plains");
					Ui.CurrentWindow().Get<ButtonWidget>("GENERATE_PRESET").OnClick();
				});
			};
			Game.OnShellmapLoaded += loaded;
		}

		public static void StartLobby(Widget lobby, ModData mod, OrderManager manager)
		{
			var output = Output();
			if (output == null) return;
			var role = Environment.GetEnvironmentVariable("YMCA_LAN_MAP_PROBE_ROLE");
			var log = Path.Combine(output, "results.txt");
			File.WriteAllText(log, "START " + role + "\n");
			void Check(bool ok, string text)
			{
				if (!ok) throw new InvalidOperationException("LAN probe: " + text);
				File.AppendAllText(log, "PASS " + text + "\n");
			}
			bool Ready() => manager.LocalClient?.State == Session.ClientState.NotReady &&
				GeneratedMapLobbyLogic.HasGeneratedMapRecipe(manager.LobbyInfo.GlobalSettings) &&
				mod.MapCache[manager.LobbyInfo.GlobalSettings.Map].Status == MapStatus.Available;
			var initiallyAvailable = new HashSet<string>(mod.MapCache.Where(m => m.Status == MapStatus.Available).Select(m => m.Uid));
			Poll(Ready, () =>
			{
				var initial = manager.LobbyInfo.GlobalSettings.Map;
				Check(!manager.LobbyInfo.GlobalSettings.Dedicated, "real peer-hosted lobby, not dedicated");
				if (role != "host") Check(!initiallyAvailable.Contains(initial), "guest starts without the host map");
				File.AppendAllText(log, "INITIAL_MAP_READY " + initial + "\n");
				void Done()
				{
					Check(role == "late" || manager.LobbyInfo.GlobalSettings.Map != initial, "selected map synchronized");
					File.AppendAllText(log, "MAP_READY " + manager.LobbyInfo.GlobalSettings.Map + "\n");
					Game.Renderer.SaveScreenshot(Path.Combine(output, "ready.png"));
					if (role == "host")
						Poll(() => File.Exists(Path.Combine(Directory.GetParent(output).FullName, "late", "map-ready.txt")),
							() => manager.IssueOrder(Order.Command("startgame")));
					if (role == "late") File.WriteAllText(Path.Combine(output, "map-ready.txt"), "OK");
					if (role == "host" || role == "late")
						Poll(() => manager.GameStarted && manager.World != null && manager.World.WorldTick >= 150,
							() =>
							{
								File.AppendAllText(log, "MATCH_STARTED " + manager.World.WorldTick + "\n");
								if (Environment.GetEnvironmentVariable("YMCA_LAN_MAP_PROBE_SAVE_REPLAY") == "1")
									Poll(() => File.ReadAllText(Path.Combine(Directory.GetParent(output).FullName, "late", "results.txt")).Contains("MATCH_STARTED"), () =>
									{
										Game.Disconnect();
										File.AppendAllText(log, "REPLAY_SAVED\n");
										Environment.SetEnvironmentVariable("YMCA_LAN_MAP_PROBE_ROLE", null);
										Game.LoadShellMap();
									});
							});
				}
				if (role == "late") { Done(); return; }
				if (role == "guest")
				{
					Check(!manager.LocalClient.IsAdmin, "guest cannot change maps");
					var options = new MapGenerationOptions { Preset = MapGenerationPreset.Arabia, Width = 96, Height = 96, Players = 4 };
					manager.IssueOrder(Order.Command(GeneratedMapRecipe.Command + initial + " " + GeneratedMapRecipe.Encode(options)));
					Game.RunAfterDelay(1500, () =>
					{
						Check(manager.LobbyInfo.GlobalSettings.Map == initial, "server rejects guest generation");
						File.WriteAllText(Path.Combine(output, "rejected.txt"), "OK");
						Poll(() => Ready() && manager.LobbyInfo.GlobalSettings.Map != initial, Done);
					});
					return;
				}
				Poll(() => manager.LobbyInfo.Clients.Count(c => c.Bot == null) >= 2 &&
					File.Exists(Path.Combine(Directory.GetParent(output).FullName, "guest", "rejected.txt")), () =>
				{
					Check(manager.LocalClient.IsAdmin, "peer host has map-change rights");
					lobby.Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					Click("Tactical"); Click("Two Sides");
					Ui.CurrentWindow().Get<ButtonWidget>("GENERATE_PRESET").OnClick();
					Poll(() => Ready() && manager.LobbyInfo.GlobalSettings.Map != initial, Done);
				});
			});
		}
	}
}
