using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Explicit opt-in only; drives real chrome callbacks and captures the GPU framebuffer.
	static class SkirmishSetupUiProbe
	{
		internal static void Start(Widget menu)
		{
			var requested = Environment.GetEnvironmentVariable("YMCA_SKIRMISH_UI_PROBE");
			if (string.IsNullOrEmpty(requested)) return;
			var preset = Environment.GetEnvironmentVariable("YMCA_SKIRMISH_UI_PROBE_PRESET") == "two-sides" ? "Two Sides" : "Mountain Valleys";
			var output = Path.GetFullPath(requested);
			var root = Path.Combine(Path.GetTempPath(), "ymca-ui-probes") + Path.DirectorySeparatorChar;
			if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("UI probe output must be below the dedicated temp directory.");
			Directory.CreateDirectory(output);
			var log = Path.Combine(output, "results.txt"); File.WriteAllText(log, "");
			OrderManager lobby = null;
			Action<OrderManager, string, NetworkConnection> connected = (manager, password, connection) => lobby = manager;
			Game.ConnectionStateChanged += connected;
			void Click(string text)
			{
				var generate = Ui.CurrentWindow().GetOrNull<ButtonWidget>("GENERATE_PRESET");
				if (text == "Generate and enter lobby" && generate != null && generate.IsVisible()) generate.OnClick();
				else Ui.CurrentWindow().Get("CONTENT").Children.OfType<ButtonWidget>().Single(b => b.GetText() == text).OnClick();
			}
			IEnumerable<Widget> Descendants(Widget widget)
			{
				yield return widget;
				foreach (var child in widget.Children) foreach (var descendant in Descendants(child)) yield return descendant;
			}
			void Choose(string label, string value)
			{
				var panel = Ui.CurrentWindow().Get("CONTENT");
				var y = panel.Children.OfType<LabelWidget>().Single(l => l.GetText() == label).Bounds.Y;
				panel.Children.OfType<DropDownButtonWidget>().Single(b => b.Bounds.Y == y).OnClick();
				Descendants(Ui.Root).OfType<ScrollItemWidget>().Single(r => r.GetOrNull<LabelWidget>("LABEL")?.GetText() == value).OnClick();
			}
			void CheckPreviews(int expected)
			{
				var content = Ui.CurrentWindow().Get("CONTENT");
				var cards = content.Children.OfType<SkirmishPresetButtonWidget>().ToArray();
				if (cards.Length != expected || cards.Any(c => c.Bounds.X < 0 || c.Bounds.Y < 0 ||
					c.Bounds.X + c.Bounds.Width > content.Bounds.Width || c.Bounds.Y + c.Bounds.Height > 385))
					throw new InvalidOperationException("Missing or overflowing preset preview cards.");
				File.AppendAllText(log, $"UI PASS {expected} preset preview cards within the existing dialog.\n");
			}
			void ClickPreview(string text)
			{
				var card = Ui.CurrentWindow().Get("CONTENT").Children.OfType<SkirmishPresetButtonWidget>().Single(b => b.GetText() == text);
				var point = new int2(card.RenderBounds.X + 40, card.RenderBounds.Y + 40);
				card.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left, point, int2.Zero, Modifiers.None, 1));
				card.HandleMouseInput(new MouseInput(MouseInputEvent.Up, MouseButton.Left, point, int2.Zero, Modifiers.None, 1));
				var panel = Ui.CurrentWindow().Get("CONTENT");
				var generate = Ui.CurrentWindow().Get<ButtonWidget>("GENERATE_PRESET");
				if (!generate.IsVisible() || panel.Children.OfType<ButtonWidget>().Where(b => b.GetText() == "Custom Map Parameters" || b.GetText() == "Hand Made Map")
					.Any(b => b.RenderBounds.Y + b.RenderBounds.Height >= generate.RenderBounds.Y) ||
					!panel.Children.OfType<SkirmishPresetButtonWidget>().Any(b => b.GetText() == text && b.IsHighlighted()) ||
					!panel.Children.OfType<LabelWidget>().Any(l => l.GetText() == "Players (automatic size)"))
					throw new InvalidOperationException("Preset selection must remain on the player-count/preset/generate page.");
			}
			void Back() => Ui.CurrentWindow().Get<ButtonWidget>("BACK").OnClick();
			void Capture(string name) => Game.Renderer.SaveScreenshot(Path.Combine(output, name + ".png"));
			var steps = new Queue<(int Delay, Action Work)>(new (int, Action)[]
			{
				(6000, () => menu.Get<ButtonWidget>("SKIRMISH_BUTTON").OnClick()),
				(1200, () => { Capture("modes"); Click("Strategic"); }),
				(1200, () =>
				{
					var texts = Ui.CurrentWindow().Get("CONTENT").Children.OfType<ButtonWidget>().Select(b => b.GetText()).ToArray();
					if (texts.Contains("Normal Map") || texts.Contains("Custom Map") || !texts.Contains("Hand Made Map") || texts.Contains("Continents"))
						throw new InvalidOperationException("Unexpected Strategic preset page.");
					if (!texts.Contains("Plains") || texts.Contains("Arabia"))
						throw new InvalidOperationException("Preset page did not apply the Plains display name.");
					CheckPreviews(5); Capture("strategic-presets"); Click("Hand Made Map");
				}),
				(1200, () =>
				{
					if (Ui.CurrentWindow().GetOrNull<ButtonWidget>("REMOTE_MAPS_TAB_BUTTON") == null) throw new InvalidOperationException("Hand Made Map did not open the chooser.");
					Capture("hand-made-chooser"); Ui.CloseWindow(); Click("Custom Map Parameters");
				}),
				(1200, () =>
				{
					var panel = Ui.CurrentWindow().Get("CONTENT");
					if (panel.Children.OfType<LabelWidget>().Any(l => l.GetText() == "Preset") ||
						!panel.Children.OfType<LabelWidget>().Any(l => l.GetText() == "Subtype"))
						throw new InvalidOperationException("Customize still exposes presets instead of subtypes.");
					if (panel.Children.OfType<DropDownButtonWidget>().Min(b => b.Bounds.Y) != panel.Children.OfType<LabelWidget>().Single(l => l.GetText() == "Players (automatic size)").Bounds.Y)
						throw new InvalidOperationException("Players is not the first Customize option.");
					if (panel.Children.OfType<DropDownButtonWidget>().Max(b => b.Bounds.Y + b.Bounds.Height) >=
						panel.Children.OfType<CheckboxWidget>().Min(b => b.Bounds.Y))
						throw new InvalidOperationException("Customize dropdowns must precede all checkboxes.");
					Capture("customize"); Click("Battle in the Center");
					if (Ui.CurrentWindow().Get("CONTENT").Children.OfType<CheckboxWidget>().Any(b => b.GetText() == "Center buildable"))
						throw new InvalidOperationException("Center toggle offered without a center.");
					Click("Battle in the Center"); Click("Center buildable");
					Back(); Back(); Click("Tactical"); Click("Custom Map Parameters");
					Choose("Archetype", "Islands");
					foreach (var subtype in new[] { "Small Islands", "Big Islands", "Mixed Sizes" })
					{
						Choose("Subtype", subtype);
						foreach (var resource in new[] { "None", "Glitter", "Patches" }) Choose("Resources", resource);
						foreach (var label in new[] { "Finite resources (no generators)", "Tech buildings" })
						{
							var checkbox = Ui.CurrentWindow().Get("CONTENT").Children.OfType<CheckboxWidget>().Single(c => c.GetText() == label);
							var before = checkbox.IsChecked(); checkbox.OnClick();
							if (checkbox.IsChecked() == before) throw new InvalidOperationException("Island checkbox did not toggle: " + label);
							checkbox.OnClick();
						}
						if (Ui.CurrentWindow().Get("CONTENT").Children.OfType<LabelWidget>().Any(l => l.GetText().StartsWith("Economy follows")))
							throw new InvalidOperationException("Obsolete island explanation is still visible.");
					}
					File.AppendAllText(log, "UI PASS players first; all island subtypes offer None/Glitter/Patches and working finite/tech toggles; explanation removed; independent center controls.\n");
				}),
				(1200, () => { Capture("customize-islands"); Back(); Back(); Click("Operational"); }),
				(1200, () =>
				{
					CheckPreviews(9); Choose("Players (automatic size)", "6"); Choose("Players (automatic size)", "4");
					Capture("operational-presets"); ClickPreview("Plains"); Back(); Click("Tactical");
				}),
				(1200, () =>
				{
					CheckPreviews(9); Capture("tactical-presets"); ClickPreview(preset); Back();
					Click("Strategic"); Click(preset);
					File.AppendAllText(log, "UI PASS direct presets, compatible choices, hand-made chooser/cancel, customize and back navigation.\n");
					Click("Generate and enter lobby");
				}),
				(Environment.GetEnvironmentVariable("YMCA_SKIRMISH_CHANGE_MAP_PROBE") == "1" ? 60000 : 15000, () =>
				{
					var clients = lobby.LobbyInfo.Clients.Where(c => !c.IsObserver).ToArray();
					if (Environment.GetEnvironmentVariable("YMCA_SKIRMISH_CHANGE_MAP_PROBE") == "1")
					{
						Capture("strategic-lobby");
						if (lobby.LobbyInfo.Clients.Count(c => c.Bot == null) != 1)
							throw new InvalidOperationException($"Missing local human in Change Map fixture: clients={lobby.LobbyInfo.Clients.Count}, map={lobby.LobbyInfo.GlobalSettings.Map}, window={Ui.CurrentWindow().Id}.");
						return;
					}
					var human = clients.Single(c => c.Bot == null); var bot = clients.Single(c => c.Bot != null);
					if (human.Team == 0 || bot.Team == 0 || human.Team == bot.Team || human.SpawnPoint == bot.SpawnPoint)
						throw new InvalidOperationException("Default Strategic bot is not an opponent on the other team.");
					Capture("strategic-lobby");
					File.AppendAllText(log, $"UI COMPLETE human team={human.Team} spawn={human.SpawnPoint}; opponent team={bot.Team} spawn={bot.SpawnPoint}.\n");
				}),
			});
			if (Environment.GetEnvironmentVariable("YMCA_SKIRMISH_CHANGE_MAP_PROBE") == "1")
			{
				string originalMap = null;
				OrderManager originalLobby = null;
				steps.Enqueue((1000, () =>
				{
					originalLobby = lobby;
					originalMap = lobby.LobbyInfo.GlobalSettings.Map;
					Ui.CurrentWindow().Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					Capture("change-map-modes");
					Back();
					if (lobby.LobbyInfo.GlobalSettings.Map != originalMap)
						throw new InvalidOperationException("Cancel changed the lobby map.");
					Ui.CurrentWindow().Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					Click("Tactical"); Click("Hand Made Map");
					if (Ui.CurrentWindow().GetOrNull<ButtonWidget>("REMOTE_MAPS_TAB_BUTTON") == null)
						throw new InvalidOperationException("Change Map lost the existing-map chooser.");
					Ui.CloseWindow(); Click("Plains"); Click("Generate and enter lobby");
				}));
				steps.Enqueue((60000, () =>
				{
					Capture("changed-map-lobby");
					var uid = lobby.LobbyInfo.GlobalSettings.Map;
					if (lobby != originalLobby || uid == originalMap ||
						!Game.ModData.MapCache[uid].Title.Contains("Plains"))
						throw new InvalidOperationException($"Change Map failed: sameLobby={lobby == originalLobby}, uid={uid}, original={originalMap}, saved={Game.Settings.Server.Map}, title={Game.ModData.MapCache[uid].Title}.");
					File.AppendAllText(log, "CHANGE MAP PASS wizard reopened, cancel retained map, hand-made chooser accessible, Plains generated in the same lobby.\n");
					Ui.CurrentWindow().Get<ButtonWidget>("CHANGEMAP_BUTTON").OnClick();
					Click("Tactical"); Click("Two Sides"); Click("Generate and enter lobby");
				}));
				steps.Enqueue((60000, () =>
				{
					var human = lobby.LobbyInfo.Clients.Single(c => c.Bot == null && !c.IsObserver);
					var bot = lobby.LobbyInfo.Clients.Single(c => c.Bot != null && !c.IsObserver);
					if (human.Team == 0 || bot.Team == human.Team || human.SpawnPoint > 2 || bot.SpawnPoint <= 2)
						throw new InvalidOperationException($"Map change put the opponent on the home bank: human={human.Team}/{human.SpawnPoint}; bot={bot.Team}/{bot.SpawnPoint}.");
					Capture("two-sides-opponent");
					File.AppendAllText(log, $"CHANGE MAP COMPLETE existing opponent retained across Plains -> Two Sides: human={human.Team}/{human.SpawnPoint}; bot={bot.Team}/{bot.SpawnPoint}.\n");
				}));
			}
			if (Environment.GetEnvironmentVariable("YMCA_SKIRMISH_DEBUG_PROBE") == "1")
			{
				var preference = Path.Combine(Platform.SupportDir, "skirmish-debug.ca.txt");
				void CheckDebug(bool expected)
				{
					if (lobby.LobbyInfo.GlobalSettings.OptionOrDefault("cheats", false) != expected ||
						!File.Exists(preference) || File.ReadAllText(preference).Trim() != expected.ToString())
						throw new InvalidOperationException("Lobby debug option and persisted preference disagree.");
					File.AppendAllText(log, $"DEBUG LOBBY PASS enabled={expected}.\n");
				}
				steps.Enqueue((1000, () => { CheckDebug(true); lobby.IssueOrder(Order.Command("option cheats False")); }));
				steps.Enqueue((1500, () => { CheckDebug(false); lobby.IssueOrder(Order.Command("option cheats True")); }));
				steps.Enqueue((1500, () =>
				{
					CheckDebug(true);
					var target = Game.ModData.MapCache.Single(m => m.Status == MapStatus.Available && m.Title == "Debug Preference Basin Fixture");
					lobby.IssueOrder(Order.Command("map " + target.Uid));
				}));
				steps.Enqueue((5000, () => { CheckDebug(true); File.AppendAllText(log, "DEBUG LOBBY COMPLETE restored on fresh wizard map, persisted both toggle values and retained on map switch.\n"); }));
			}
			void Next()
			{
				if (steps.Count == 0) { Game.ConnectionStateChanged -= connected; return; }
				var step = steps.Dequeue();
				Game.RunAfterDelay(step.Delay, () =>
				{
					try { step.Work(); Next(); }
					catch (Exception e) { Game.ConnectionStateChanged -= connected; File.AppendAllText(log, "FAIL " + e + "\n"); }
				});
			}
			Next();
		}
	}
}
