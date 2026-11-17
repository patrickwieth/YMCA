using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Opt-in loopback diagnostic only. Does not run in ordinary clients.
	static class DedicatedMapGeneratorProbe
	{
		public static void Start(Widget lobby, ModData mod, OrderManager manager)
		{
			var output = Environment.GetEnvironmentVariable("YMCA_DEDICATED_MAP_PROBE");
			var role = Environment.GetEnvironmentVariable("YMCA_DEDICATED_MAP_PROBE_ROLE");
			if (string.IsNullOrEmpty(output) || !new[] { "host", "guest", "late" }.Contains(role)) return;
			output = Path.GetFullPath(output);
			var root = Path.Combine(Path.GetTempPath(), "ymca-dedicated-map-probes") + Path.DirectorySeparatorChar;
			if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Probe output must be in the dedicated temp directory.");
			Directory.CreateDirectory(output);
			var log = Path.Combine(output, "results.txt");
			File.WriteAllText(log, "START " + role + "\n");
			var initialMap = manager.LobbyInfo.GlobalSettings.Map;
			void Record(string text) => File.AppendAllText(log, text + "\n");
			void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException("Dedicated map probe: " + text); Record("PASS " + text); }
			void Click(string text) => Ui.CurrentWindow().Get("CONTENT").Children.OfType<ButtonWidget>().Single(b => b.GetText() == text).OnClick();
			void Poll(Func<bool> ready, Action action, int attempts = 500)
			{
				Game.RunAfterDelay(500, () =>
				{
					if (ready()) action();
					else if (attempts > 0) Poll(ready, action, attempts - 1);
					else throw new InvalidOperationException("Dedicated map probe timed out: " + role);
				});
			}

			void WaitForMap() => Poll(() => GeneratedMapLobbyLogic.HasGeneratedMapRecipe(manager.LobbyInfo.GlobalSettings) &&
				mod.MapCache[manager.LobbyInfo.GlobalSettings.Map].Status == MapStatus.Available && manager.LocalClient?.State == Session.ClientState.NotReady,
				() =>
				{
					Check(role == "late" || manager.LobbyInfo.GlobalSettings.Map != initialMap, "server changed the existing lobby map");
					Record("MAP_READY " + manager.LobbyInfo.GlobalSettings.Map);
					Game.Renderer.SaveScreenshot(Path.Combine(output, "ready.png"));
				});

			Poll(() => manager.LocalClient != null && !manager.LocalClient.IsReady &&
				manager.LobbyInfo.GlobalSettings.LobbyOptions.ContainsKey(GeneratedMapRecipe.Capability), () =>
			{
				initialMap = manager.LobbyInfo.GlobalSettings.Map;
				if (role == "late") { WaitForMap(); return; }
				if (role == "guest")
				{
					Check(!manager.LocalClient.IsAdmin, "guest has no map-change rights");
					lobby.Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					Check(Ui.CurrentWindow().GetOrNull<ButtonWidget>("REMOTE_MAPS_TAB_BUTTON") != null, "guest retains read-only map chooser");
					Ui.CloseWindow();
					var options = new MapGenerationOptions { Preset = MapGenerationPreset.Arabia, Width = 96, Height = 96, Players = 4 };
					manager.IssueOrder(Order.Command(GeneratedMapRecipe.Command + initialMap + " " + GeneratedMapRecipe.Encode(options)));
					Game.RunAfterDelay(6000, () =>
					{
						Check(manager.LobbyInfo.GlobalSettings.Map == initialMap, "server rejected non-admin generation command");
						WaitForMap();
					});
					return;
				}

				Poll(() => manager.LobbyInfo.Clients.Count(c => c.Bot == null) >= 2, () => Game.RunAfterDelay(12000, () =>
				{
					Check(manager.LocalClient.IsAdmin, "remote lobby admin has map-change rights");
					lobby.Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					foreach (var mode in new[] { "Tactical", "Operational", "Strategic" })
					{
						Click(mode);
						Check(Ui.CurrentWindow().Get("CONTENT").Children.OfType<SkirmishPresetButtonWidget>().Any(), mode + " generator options visible");
						Click("Custom Map Parameters");
						Check(Ui.CurrentWindow().Get("CONTENT").Children.OfType<LabelWidget>().Any(l => l.GetText() == "Archetype"), mode + " custom parameters visible");
						Ui.CurrentWindow().Get<ButtonWidget>("BACK").OnClick();
						Ui.CurrentWindow().Get<ButtonWidget>("BACK").OnClick();
					}

					Click("Tactical");
					Click("Hand Made Map");
					Check(Ui.CurrentWindow().GetOrNull<ButtonWidget>("REMOTE_MAPS_TAB_BUTTON") != null, "admin retains the server's ordinary map chooser");
					Ui.CloseWindow();
					lobby.Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					Click("Tactical");
					Click("Plains");
					Game.Renderer.SaveScreenshot(Path.Combine(output, "generator-options.png"));
					Ui.CurrentWindow().Get<ButtonWidget>("GENERATE_PRESET").OnClick();
					WaitForMap();
				}));
			});
		}
	}
}
