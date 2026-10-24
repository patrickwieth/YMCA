using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.UtilityCommands;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class MainMenuLogicCA : MainMenuLogic
	{
		readonly Widget menu;
		[ObjectCreator.UseCtor]
		public MainMenuLogicCA(Widget widget, World world, ModData modData) : base(widget, world, modData)
		{
			menu = widget;
			var button = widget.Get<ButtonWidget>("SKIRMISH_BUTTON");
			button.Disabled = false;
			button.OnClick = () => Game.OpenWindow("SKIRMISH_SETUP", new WidgetArgs
			{
				{ "onSelect", (Action<string>)StartConfiguredSkirmish },
			});
			SkirmishSetupUiProbe.Start(menu);
		}

		// Also used by the designer: its frozen map must bypass the map wizard.
		public void StartConfiguredSkirmish(string map)
		{
			menuType = MenuType.None;
			Game.Settings.Server.Map = map;
			Game.Settings.Save();
			ConnectionLogic.Connect(Game.CreateLocalServer(map, isSkirmish: true), "",
				() => Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
				{
					{ "onExit", (Action)(() => { Game.Disconnect(); menuType = MenuType.Singleplayer; }) },
					{ "onStart", (Action)(() => { menu.Parent?.RemoveChild(menu); lastGameState = MenuPanel.Skirmish; }) },
					{ "skirmishMode", true },
				}),
				() => { Game.CloseServer(); menuType = MenuType.Singleplayer; });
		}
	}

	public class SkirmishSetupLogic : ChromeLogic
	{
		readonly Widget content;
		readonly ModData modData;
		readonly Action<string> onSelect;
		GeneratedMapMode mode;
		MapGenerationPreset preset;
		MapGenerationOptions options;
		CustomMapSettings customSettings = new CustomMapSettings();
		int players = 4;
		int stage;
		bool customize;
		bool busy;
		string status = "";

		[ObjectCreator.UseCtor]
		public SkirmishSetupLogic(Widget widget, ModData modData, Action<string> onSelect)
		{
			this.modData = modData;
			this.onSelect = onSelect;
			content = widget.Get("CONTENT");
			widget.Get<ButtonWidget>("BACK").OnClick = () =>
			{
				if (busy) return;
				if (stage == 0) Ui.CloseWindow();
				else
				{
					stage--;
					if (stage == 1) { customize = false; ResetOptions(); }
					Show();
				}
			};
			widget.Get<ButtonWidget>("BACK").IsDisabled = () => busy;
			widget.Get<LabelWidget>("STATUS").GetText = () => status;
			var generate = widget.Get<ButtonWidget>("GENERATE_PRESET");
			generate.IsVisible = () => stage == 1;
			generate.IsDisabled = () => busy;
			generate.OnClick = Generate;
			Show();
		}

		static bool Compatible(GeneratedMapMode mode, MapGenerationPreset preset) => mode != GeneratedMapMode.Strategic ||
			(preset != MapGenerationPreset.BattleNexus && preset != MapGenerationPreset.Continents &&
			preset != MapGenerationPreset.Archipelago && preset != MapGenerationPreset.Migration);

		bool Matches(MapPreview map)
		{
			bool Has(string value) => map.Categories.Contains(value, StringComparer.OrdinalIgnoreCase);
			return mode == GeneratedMapMode.Tactical ? !Has("Operational") && !Has("Strategic") : Has(mode.ToString());
		}

		static string Title(object value) => System.Text.RegularExpressions.Regex.Replace(
			value is MapGenerationPreset mapPreset ? mapPreset.DisplayName() : value.ToString(), "([a-z])([A-Z])", "$1 $2");

		void Label(string text, int x, int y, int width = 790)
		{
			content.AddChild(new LabelWidget(modData) { Bounds = new WidgetBounds(x, y, width, 28), GetText = () => text });
		}

		void Button(string text, int x, int y, int width, Action action)
		{
			content.AddChild(new ButtonWidget(modData)
			{
				Bounds = new WidgetBounds(x, y, width, 40), GetText = () => text,
				OnClick = action, IsDisabled = () => busy,
			});
		}

		void DropDown<T>(string label, int y, T[] values, Func<T> selected, Action<T> choose)
		{
			Label(label, 0, y, 210);
			var button = new DropDownButtonWidget(modData) { Bounds = new WidgetBounds(220, y, 400, 30), GetText = () => Title(selected()) };
			button.IsDisabled = () => busy;
			button.OnClick = () => button.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 300, values, (item, template) =>
			{
				var row = ScrollItemWidget.Setup(template, () => Equals(item, selected()), () => choose(item));
				row.Get<LabelWidget>("LABEL").GetText = () => Title(item);
				return row;
			});
			content.AddChild(button);
		}

		void Check(string text, int y, Func<bool> selected, Action toggle)
		{
			content.AddChild(new CheckboxWidget(modData)
			{
				Bounds = new WidgetBounds(220, y, 510, 28), GetText = () => text,
				IsChecked = selected, OnClick = toggle, IsDisabled = () => busy,
			});
		}

		void ResetOptions()
		{
			if (customize) preset = customSettings.BasePreset;
			options = DebugMapGeneratorCommand.ParseOptions(new[] { "setup", ".", "mode=" + mode, "preset=" + preset,
				"players=" + players, "teams=2", "seed=" + Game.CosmeticRandom.Next(1, int.MaxValue), "size=auto" });
			if (customize)
			{
				options.Custom = customSettings.Copy();
				customSettings.ApplyAutomaticSize(options);
			}
		}

		void SelectMode(GeneratedMapMode selectedMode)
		{
			mode = selectedMode;
			customize = false;
			if (!Compatible(mode, preset)) preset = MapGenerationPreset.TwoSides;
			ResetOptions(); stage = 1; Show();
		}

		void Show()
		{
			content.RemoveChildren();
			status = "";
			Label($"{stage + 1}. Skirmish setup" + (stage > 0 ? " - " + mode : ""), 0, 0);
			if (stage == 0)
			{
				var descriptions = new[] { "Direct unit control", "Command through army leaders", "Two teams, ordered checkpoints" };
				for (var i = 0; i < 3; i++)
				{
					var choice = (GeneratedMapMode)i;
					content.AddChild(new SkirmishModePreviewWidget
					{
						Bounds = new WidgetBounds(i * 270, 65, 250, 155), Mode = i,
						OnClick = () => SelectMode(choice),
					});
					Button(choice.ToString(), i * 270, 235, 250, () => SelectMode(choice));
					Label(descriptions[i], i * 270, 285, 265);
				}
			}
			else if (stage == 1)
			{

				DropDown("Players (automatic size)", 38, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16 }, () => players,
					value => { var seed = options.Seed; players = value; ResetOptions(); options.Seed = seed; Show(); });
				// UI order only: keep serialized preset values and generator dispatch unchanged.
				var presets = Enum.GetValues<MapGenerationPreset>().Select(p => p switch
				{
					MapGenerationPreset.MountainValleys => MapGenerationPreset.BattleNexus,
					MapGenerationPreset.BattleNexus => MapGenerationPreset.MountainValleys,
					_ => p,
				}).Where(p => Compatible(mode, p)).ToArray();
				for (var i = 0; i < presets.Length; i++)
				{
					var choice = presets[i];
					content.AddChild(new SkirmishPresetButtonWidget(modData, choice)
					{
						Bounds = new WidgetBounds(i % 3 * 270, 76 + i / 3 * 102, 250, 100),
						GetText = () => Title(choice), IsDisabled = () => busy,
						IsHighlighted = () => !customize && preset == choice,
						OnClick = () => { preset = choice; customize = false; ResetOptions(); Show(); },
					});
				}
				Button("Custom Map Parameters", 135, 385, 250, () =>
				{
					if (!customSettings.Compatible(mode)) customSettings = new CustomMapSettings();
					customize = true; ResetOptions(); stage = 2; Show();
				});
				Button("Hand Made Map", 405, 385, 250, ChooseMap);
			}
			else ShowCustomization();
		}

		void ChangeCustom(Action change)
		{
			var previous = customSettings.Copy();
			change();
			if (!customSettings.Compatible(mode))
			{
				customSettings = previous;
				status = "This topology combination is unavailable in Strategic mode.";
				return;
			}
			if (previous.Topology != customSettings.Topology || previous.HasCenter != customSettings.HasCenter)
				customSettings.ResetCenterDefault();
			var oldOptions = options;
			ResetOptions();
			options.Seed = oldOptions.Seed;
			if (previous.Topology == customSettings.Topology && customSettings.Topology != CustomTopology.Islands)
			{
				options.Resources = oldOptions.Resources;
				options.FiniteResources = oldOptions.FiniteResources;
				options.TechBuildings = oldOptions.TechBuildings;
			}
			if (previous.Topology == CustomTopology.Islands && customSettings.Topology == CustomTopology.Islands)
			{
				options.Resources = oldOptions.Resources;
				options.FiniteResources = oldOptions.FiniteResources;
				if (oldOptions.TechBuildings == TechBuildingDensity.None) options.TechBuildings = TechBuildingDensity.None;
				if (options.Resources == ResourceFieldLayout.None) options.FiniteResources = true;
			}
			Show();
		}

		void ShowCustomization()
		{
			DropDown("Players (automatic size)", 38, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16 }, () => players,
				value => { var old = options; players = value; ResetOptions(); old.Players = players; old.Width = options.Width; old.Height = options.Height; options = old; Show(); });
			DropDown("Archetype", 76, Enum.GetValues<CustomTopology>().Where(t => mode != GeneratedMapMode.Strategic || t != CustomTopology.Islands).ToArray(),
				() => customSettings.Topology, t => ChangeCustom(() => customSettings.Topology = t));
			if (customSettings.Topology == CustomTopology.Strongholds)
			{
				DropDown("Subtype", 114, Enum.GetValues<StrongholdSubtype>(), () => customSettings.Strongholds, t => ChangeCustom(() => customSettings.Strongholds = t));
				Check("Connected Start Areas", 194, () => customSettings.ConnectedStartAreas, () => ChangeCustom(() => customSettings.ConnectedStartAreas = !customSettings.ConnectedStartAreas));
				Check("Battle in the Center", 228, () => customSettings.BattleInTheCenter, () => ChangeCustom(() => customSettings.BattleInTheCenter = !customSettings.BattleInTheCenter));
			}
			else if (customSettings.Topology == CustomTopology.OpenPlains)
				DropDown("Subtype", 114, Enum.GetValues<PlainsSubtype>(), () => customSettings.Plains, t => ChangeCustom(() => customSettings.Plains = t));
			else
			{
				DropDown("Subtype", 114, Enum.GetValues<IslandSubtype>(), () => customSettings.Islands, t => ChangeCustom(() => customSettings.Islands = t));
				Check("Connections", 194, () => customSettings.Connections, () => ChangeCustom(() => customSettings.Connections = !customSettings.Connections));
			}
			if (customSettings.HasCenter)
				Check("Center buildable", 262, () => customSettings.CenterBuildable, () => ChangeCustom(() => customSettings.CenterBuildable = !customSettings.CenterBuildable));
			DropDown("Resources", 152, Enum.GetValues<ResourceFieldLayout>(), () => options.Resources, value =>
			{
				options.Resources = value;
				if (value == ResourceFieldLayout.None) options.FiniteResources = true;
			});
			Check("Finite resources (no generators)", 296, () => options.FiniteResources, () => { if (options.Resources != ResourceFieldLayout.None) options.FiniteResources = !options.FiniteResources; });
			Check("Tech buildings", 330, () => options.TechBuildings != TechBuildingDensity.None,
				() => options.TechBuildings = options.TechBuildings != TechBuildingDensity.None ? TechBuildingDensity.None :
					customSettings.Topology == CustomTopology.Islands && options.Preset != MapGenerationPreset.Continents ? TechBuildingDensity.Dense : TechBuildingDensity.Sparse);
			Label($"{options.Width} x {options.Height} / 2 teams / seed {options.Seed}", 220, 355, 590);
			Button("Generate and enter lobby", 220, 385, 400, Generate);
		}

		void ChooseMap()
		{
			var pool = new HashSet<string>(modData.MapCache.Where(m => m.Status == MapStatus.Available &&
				m.Visibility.HasFlag(MapVisibility.Lobby) && Matches(m)).Select(m => m.Uid));
			if (pool.Count == 0) { status = "No installed maps match " + mode + ". Select a preset to generate one."; return; }
			// Reuse the engine's restricted-map-pool support, including its random selector.
			Game.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs
			{
				{ "initialMap", pool.First() }, { "remoteMapPool", pool }, { "initialTab", MapClassification.Remote },
				{ "filter", MapVisibility.Lobby },
				{ "onExit", (Action)(() => { }) },
				{ "onSelect", (Action<string>)(uid => { if (uid != null && pool.Contains(uid) && Matches(modData.MapCache[uid])) { Ui.CloseWindow(); onSelect(uid); } }) },
			});
			Ui.CurrentWindow().Get<ButtonWidget>("REMOTE_MAPS_TAB_BUTTON").GetText = () => mode + " Maps";
		}

		void Generate()
		{
			busy = true;
			status = "Generating temporary map (removed when exiting the game)...";
			// Export uses engine resources: keep it on the game thread, after the UI updates.
			Game.RunAfterDelay(100, () =>
			{
				try
				{
					var folder = modData.MapCache.MapLocations.First(p => p.Value == MapClassification.User);
					var plan = MapPlanGenerator.Generate(options);
					var path = new RubberduckMapExporter(modData).Export(plan, options, folder.Key.Name, temporary: true);
					TemporaryGeneratedMaps.Register(path);
					modData.MapCache.LoadMap(Path.GetFileName(path), folder.Key, folder.Value, modData.Manifest.Get<MapGrid>(), null);
					var map = modData.MapCache.First(m => m.Status == MapStatus.Available && string.Equals(Path.GetFullPath(m.PackageName), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
					Ui.CloseWindow();
					onSelect(map.Uid);
				}
				catch (Exception e) { status = "Generation failed: " + e.Message; Log.Write("debug", e); }
				finally { busy = false; }
			});
		}
	}

	public class SkirmishModePreviewWidget : Widget
	{
		public int Mode;
		public Action OnClick = () => { };

		public override bool HandleMouseInput(MouseInput input)
		{
			if (input.Button != MouseButton.Left || !RenderBounds.Contains(input.Location)) return false;
			if (input.Event == MouseInputEvent.Up) OnClick();
			return true;
		}

		public override void Draw()
		{
			var r = RenderBounds;
			WidgetUtils.FillRectWithColor(r, Color.FromArgb(255, 25, 38, 45));
			for (var team = 0; team < 2; team++)
				for (var i = 0; i < 6; i++)
				{
					var size = Mode == 1 && i == 0 ? 24 : 10;
					var x = r.X + 25 + team * 135 + i % 3 * 18;
					var y = r.Y + 40 + i / 3 * 32;
					WidgetUtils.FillRectWithColor(new Rectangle(x, y, size, size), team == 0 ? Color.Red : Color.CornflowerBlue);
				}
			if (Mode == 2)
				for (var i = 0; i < 3; i++)
					WidgetUtils.FillRectWithColor(new Rectangle(r.X + 85 + i * 30, r.Y + 115, 15, 15), Color.Gold);
		}
	}
}
