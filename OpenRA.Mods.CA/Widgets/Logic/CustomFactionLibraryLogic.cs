using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public sealed class CustomFactionLibraryLogic : ChromeLogic
	{
		readonly Widget widget, body;
		readonly ModData modData;
		readonly Action onExit;
		readonly Action<string> onPlay;
		readonly bool playMode;
		readonly CustomFactionDesign compiler;
		readonly CustomFactionLibrary library;
		string message = "";
		Action back;

		[ObjectCreator.UseCtor]
		public CustomFactionLibraryLogic(Widget widget, ModData modData, bool playMode, Action onExit, Action<string> onPlay)
		{
			this.widget = widget; this.modData = modData; this.playMode = playMode;
			this.onExit = onExit; this.onPlay = onPlay;
			body = widget.Get("BODY");
			widget.Get<LabelWidget>("STATUS").GetText = () => WidgetUtils.TruncateText(message, 852, Game.Renderer.Fonts["Small"]);
			back = () => { Ui.CloseWindow(); onExit(); };
			widget.Get<ButtonWidget>("BACK").OnClick = () => back();
			try
			{
				compiler = CustomFactionNavigation.Compiler(modData);
				library = new CustomFactionLibrary(compiler, Path.Combine(Platform.SupportDir, "Modular", "Factions"));
				string warning = null;
				try { library.ImportLegacyOnce(Path.Combine(Platform.SupportDir, "Modular", "custom-faction.json")); }
				catch (Exception e) { warning = "Legacy profile was preserved but could not be imported: " + e.Message; }
				List();
				if (warning != null) message = warning;
			}
			catch (Exception e) { message = "Could not open the faction library: " + e.Message; }
		}

		void Run(Action action) { try { action(); } catch (Exception e) { message = e.Message; } }
		void Page(string title, Action goBack)
		{
			body.RemoveChildren(); message = ""; back = goBack;
			widget.Get<LabelWidget>("TITLE").GetText = () => title;
		}
		LabelWidget Label(Widget parent, string text, int x, int y, int width, string font = "Regular")
		{
			var label = new LabelWidget(modData) { Bounds = new WidgetBounds(x, y, width, 24), Font = font };
			label.GetText = () => WidgetUtils.TruncateText(text, width, Game.Renderer.Fonts[font]);
			parent.AddChild(label); return label;
		}
		ButtonWidget Button(Widget parent, string text, int x, int y, int width, Action click)
		{
			var button = new ButtonWidget(modData) { Bounds = new WidgetBounds(x, y, width, 28), Text = text, OnClick = () => Run(click) };
			parent.AddChild(button); return button;
		}
		TextFieldWidget Text(Widget parent, string text, int x, int y, int width)
		{
			var field = new TextFieldWidget { Bounds = new WidgetBounds(x, y, width, 28), Text = text, MaxLength = 32 };
			parent.AddChild(field); return field;
		}
		ScrollPanelWidget Scroll(int y, int height)
		{
			var panel = new ScrollPanelWidget(modData) { Bounds = new WidgetBounds(0, y, 852, height) };
			body.AddChild(panel); return panel;
		}
		static ContainerWidget Row(ScrollPanelWidget panel, int height)
		{
			var row = new ContainerWidget { Bounds = new WidgetBounds(0, 0, 820, height) };
			panel.AddChild(row); return row;
		}

		void List()
		{
			Page(playMode ? "Play - choose a faction" : "My factions", () => { Ui.CloseWindow(); onExit(); });
			if (playMode) Button(body, "New faction", 0, 0, 180, Create);
			else Label(body, "Open a faction to manage its catalog. Create new factions through Play.", 0, 0, 840, "Small");
			var entries = library.Entries();
			var list = Scroll(44, 488);
			if (entries.Length == 0) Label(Row(list, 50), playMode ? "No saved factions yet. Choose New faction to begin." :
				"No saved factions yet. Use Play > New faction to begin.", 12, 8, 790);
			foreach (var entry in entries)
			{
				var row = Row(list, 76);
				var name = entry.Roster?.Name ?? "Unreadable faction";
				var open = Button(row, name, 12, 6, 610, () => Overview(entry.Path));
				open.Disabled = entry.Roster == null;
				Label(row, entry.Roster == null ? entry.Error :
					$"Level {entry.Roster.Level}  |  Catalog: {compiler.ValidateRoster(entry.Roster)}/{entry.Roster.Level} points  |  Vehicles: {entry.Roster.Designs.Count}", 12, 39, 610, "Small");
				Button(row, "Delete", 662, 7, 132, () => ConfirmationDialogs.ButtonPrompt(modData,
					"Delete faction", $"Delete '{name}' from your saved factions? A recovery copy is kept in the Deleted folder. Existing test maps are unchanged.",
					onConfirm: () => Run(() => { library.Delete(entry.Path); List(); }), confirmText: "Delete faction",
					onCancel: () => { }, cancelText: "Cancel"));
			}
		}

		void Create()
		{
			if (!playMode) return;
			Page("Play - create a faction", () => Run(List));
			Label(body, "Faction name", 0, 24, 150);
			var name = Text(body, "My faction", 160, 24, 500);
			Label(body, "Base faction", 0, 74, 150);
			var basis = new DropDownButtonWidget(modData) { Bounds = new WidgetBounds(160, 74, 500, 28) };
			var baseId = CustomFactionDesign.BaseFactions[0];
			basis.GetText = () => CustomFactionDesign.BaseLabel(baseId);
			basis.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(string id, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => baseId == id, () => baseId = id);
					item.Get<LabelWidget>("LABEL").GetText = () => CustomFactionDesign.BaseLabel(id); return item;
				}
				basis.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, CustomFactionDesign.BaseFactions, Setup);
			};
			body.AddChild(basis);
			Label(body, "Current balance setup: level 50 / 100, 50 catalog points. One stock vehicle is included.", 0, 132, 840, "Small");
			Label(body, "Level progression is not implemented yet. Custom factions currently play on the test map.", 0, 158, 840, "Small");
			Button(body, "Create faction", 160, 212, 240, () => Overview(library.Create(name.Text, baseId).Path));
		}

		void Overview(string path)
		{
			var roster = library.Load(path);
			Page("Faction overview", () => Run(List));
			Label(body, "Faction name", 0, 0, 140);
			var name = Text(body, roster.Name, 146, 0, 482);
			void Navigate(Action action)
			{
				if (name.Text == roster.Name) { Run(action); return; }
				ConfirmationDialogs.ButtonPrompt(modData, "Unsaved name", "Discard the unsaved faction name?",
					onConfirm: () => Run(action), confirmText: "Discard", onCancel: () => { }, cancelText: "Cancel");
			}
			back = () => Navigate(List);
			Button(body, "Save name", 648, 0, 190, () =>
			{
				var copy = compiler.DeserializeRoster(compiler.SerializeRoster(roster));
				copy.Name = name.Text.Trim(); library.Update(path, copy); Overview(path); message = "Faction name saved.";
			});
			var points = compiler.ValidateRoster(roster);
			Label(body, $"Level {roster.Level} / 100  |  Catalog points: {points} / {roster.Level}  |  Available: {roster.Level - points}", 0, 38, 840, "Bold");
			Label(body, $"Base: {CustomFactionDesign.BaseLabel(roster.BaseFaction)}  |  Prototype vehicle limit: {CustomFactionDesign.MaxDesigns}", 0, 65, 840, "Small");
			foreach (var (category, i) in new[] { "Vehicles", "Infantry", "Ships", "Aircraft", "Buildings" }.Select((c, i) => (c, i)))
			{
				var categoryButton = Button(body, category, i * 170, 98, 158, () => { });
				categoryButton.Disabled = i != 0;
				categoryButton.IsHighlighted = () => i == 0;
			}
			void Change(Action<CustomFactionRoster> change)
			{
				var copy = compiler.DeserializeRoster(compiler.SerializeRoster(roster));
				change(copy); library.Update(path, copy); Overview(path);
			}
			Button(body, "New vehicle", 0, 140, 164, () => Navigate(() =>
			{
				var copy = compiler.DeserializeRoster(compiler.SerializeRoster(roster));
				compiler.AddDesign(copy);
				Edit(path, copy, copy.Designs.Count - 1, true);
			})).IsDisabled = () => roster.Designs.Count >= CustomFactionDesign.MaxDesigns;
			Button(body, "Add stock templates", 180, 140, 212, () => Navigate(() => Change(r => compiler.AddTemplates(r))));
			Button(body, "Play - test map", 604, 140, 234, () => Navigate(() =>
			{
				var uid = CustomFactionNavigation.PrepareGame(modData, compiler, roster);
				modData.MapCache.PickLastModifiedMap(MapVisibility.Lobby);
				Ui.CloseWindow(); onPlay(uid);
			}));
			var list = Scroll(182, 324);
			foreach (var (design, index) in roster.Designs.Select((d, i) => (d, i)))
			{
				var row = Row(list, 68);
				var value = compiler.Calculate(compiler.Profile(roster, design));
				Label(row, design.Name, 12, 3, 470, "Bold");
				Label(row, $"Tech {value.Tech}  |  {value.Points} catalog points  |  {value.Cost:0} credits", 12, 30, 470, "Small");
				Button(row, "Edit", 492, 12, 92, () => Navigate(() => Edit(path, roster, index, false)));
				Button(row, "Copy", 594, 12, 92, () => Navigate(() => Change(r => compiler.AddDesign(r, r.Designs[index]))))
					.IsDisabled = () => roster.Designs.Count >= CustomFactionDesign.MaxDesigns;
				Button(row, "Remove", 696, 12, 106, () => Navigate(() => ConfirmationDialogs.ButtonPrompt(modData,
					"Remove vehicle", $"Remove '{design.Name}' from this faction?", onConfirm: () => Run(() => Change(r => r.Designs.RemoveAt(index))),
					confirmText: "Remove", onCancel: () => { }, cancelText: "Cancel"))).IsDisabled = () => roster.Designs.Count <= 1;
			}
			Label(body, "Vehicles only for now. Infantry, ships, aircraft, buildings and the custom tech tree will follow.", 0, 514, 850, "Small");
		}

		void Edit(string path, CustomFactionRoster roster, int index, bool newDesign)
		{
			var session = new CustomFactionEditorSession { Roster = roster, Selected = index, NewDesign = newDesign,
				Save = r => library.Update(path, r) };
			Game.OpenWindow("CUSTOM_FACTION_PANEL", new WidgetArgs
			{
				{ "editorSession", session },
				{ "onExit", (Action)(() => { widget.Visible = true; Run(() => Overview(path)); }) },
				{ "onPlay", (Action<string>)(uid => { Ui.CloseWindow(); onPlay(uid); }) }
			});
			widget.Visible = false;
		}
	}
}
