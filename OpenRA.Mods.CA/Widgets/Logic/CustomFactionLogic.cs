using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public sealed class CustomFactionMenuLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public CustomFactionMenuLogic(Widget widget)
		{
			widget.Get<ButtonWidget>("CUSTOM_FACTION_BUTTON").OnClick = () =>
			{
				widget.Visible = false;
				Game.OpenWindow("CUSTOM_FACTION_PANEL", new WidgetArgs
				{
					{ "onExit", () => widget.Visible = true },
					{ "onPlay", (Action<string>)(uid =>
						{
							widget.Visible = true;
							widget.LogicObjects.OfType<MainMenuLogicCA>().Single().StartConfiguredSkirmish(uid);
						}) }
				});
			};
		}
	}

	public sealed class CustomFactionLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public CustomFactionLogic(Widget widget, ModData modData, Action onExit, Action<string> onPlay)
		{
			string message = "Configure a vehicle here. Add templates to include more stock families.";
			widget.Get<LabelWidget>("STATUS").GetText = () => WidgetUtils.TruncateText(message, 788, Game.Renderer.Fonts["Small"]);
			bool dirty = false;
			void DiscardThen(Action action)
			{
				if (!dirty) { action(); return; }
				ConfirmationDialogs.ButtonPrompt(modData, "Unsaved changes", "Discard changes to this faction?",
					onConfirm: action, confirmText: "Discard", onCancel: () => { }, cancelText: "Cancel");
			}
			widget.Get<ButtonWidget>("BACK").OnClick = () => DiscardThen(() => { Ui.CloseWindow(); onExit(); });
			CustomFactionDesign compiler;
			try
			{
				using var stream = modData.DefaultFileSystem.Open("ca|modular/designer-catalog.json");
				using var reader = new StreamReader(stream);
				compiler = new CustomFactionDesign(reader.ReadToEnd());
			}
			catch (Exception e)
			{
				message = "Component catalog could not be loaded: " + e.Message;
				foreach (var id in new[] { "SAVE", "PLAY", "ADD", "COPY", "DELETE", "TEMPLATES", "BASE" })
					widget.Get<ButtonWidget>(id).Disabled = true;
				return;
			}

			var profilePath = Path.Combine(Platform.SupportDir, "Modular", "custom-faction.json");
			var libraryPath = Path.Combine(Platform.SupportDir, "Modular", "Factions");
			var roster = new CustomFactionRoster();
			var loadedRoster = false;
			try
			{
				if (File.Exists(profilePath)) { roster = compiler.DeserializeRoster(File.ReadAllText(profilePath)); loadedRoster = true; }
			}
			catch (Exception e) { message = "Profile could not be loaded; showing defaults. The old file is preserved until saving. " + e.Message; }

			CustomVehicleSpaceLogic space = null;
			var preview = widget.Get<CustomVehiclePreviewWidget>("VEHICLE_PREVIEW");
			preview.IsVisible = () => space?.HasChassis ?? false;
			widget.Get<LabelWidget>("PREVIEW_STATUS").GetText = () => space?.HasChassis == true ? preview.Status : "Choose a chassis to preview.";
			var rotation = widget.Get<ButtonWidget>("PREVIEW_ROTATION");
			rotation.GetText = () => preview.Rotating ? "Pause rotation" : "Rotate slowly";
			rotation.OnClick = () => preview.Rotating = !preview.Rotating;
			var selected = 0;
			var factionName = widget.Get<TextFieldWidget>("FACTION_NAME");
			var tankName = widget.Get<TextFieldWidget>("TANK_NAME");
			factionName.Text = roster.Name;
			tankName.Text = roster.Designs[selected].Name;
			CustomFactionProfile Profile() => compiler.Profile(roster, roster.Designs[selected]);
			bool valid = false;
			var propertyPanel = widget.Get<ScrollPanelWidget>("PROPERTIES");
			propertyPanel.IsVisible = () => space?.HasChassis ?? false;
			void Properties(IEnumerable<(string Key, string Value)> values)
			{
				propertyPanel.RemoveChildren();
				foreach (var (key, value) in values)
				{
					var wrapped = WidgetUtils.WrapText($"{key}: {value}", 224, Game.Renderer.Fonts["Small"]);
					var height = Math.Max(18, Game.Renderer.Fonts["Small"].Measure(wrapped).Y);
					var row = new ContainerWidget { Bounds = new WidgetBounds(0, 0, 244, height + 4) };
					row.AddChild(new LabelWidget(modData) { Bounds = new WidgetBounds(8, 2, 224, height), Font = "Small", GetText = () => wrapped });
					propertyPanel.AddChild(row);
				}
			}
			void Refresh(bool edited)
			{
				roster.Name = factionName.Text;
				roster.Designs[selected].Name = tankName.Text;
				if (edited) dirty = true;
				try
				{
					var budget = compiler.ValidateRoster(roster);
					var p = Profile();
					var v = compiler.Calculate(p);
					preview.SetVehicle(compiler.PreviewActor(p), p.BaseFaction);
					var values = new List<(string, string)>
					{
						("Price", $"{v.Cost:0} credits"), ("Hitpoints", $"{v.Hp:0}"), ("Speed", v.Speed.ToString()),
						("Turn speed", v.Turn.ToString()), ("Mass", $"{v.Mass:0} kg"), ("Technology", v.Tech.ToString()),
						("Electrical demand (experimental)", $"{v.Electric:0.##} kW"), ("Drive reserve (experimental)", $"{v.Reserve:0.##} kW"),
						("Development points", $"{v.Points}; faction total {budget}/50"), ("Command points", "0"),
						("Faction", CustomFactionDesign.BaseLabel(p.BaseFaction)),
						("Production", v.Stationary ? "Stationary; preplaced + manual Carryall only." : "Buildable with original prerequisites."),
						("Graphics", p.Parts["running_gear"] == "prototype-hover" && !compiler.IsNativeHover(p) ? "Hover placeholder graphics." : "Bound original actor graphics.")
					};
					var names = new[] { "Chassis", "Running gear", "Engine", "Generator", "Armor", "Turret / mount", "Weapon", "Ammunition" };
					for (var i = 0; i < CustomFactionDesign.Roles.Length; i++) values.Add((names[i], compiler.Label(p.Parts[CustomFactionDesign.Roles[i]])));
					values.Add(("Original weapon / ability package", compiler.WeaponSummary(p)));
					Properties(values);
					valid = true;
					if (edited) message = "Unsaved changes. All " + roster.Designs.Count + " designs will be included in the test game.";
				}
				catch (Exception e) { Properties(new[] { ("Validation", e.Message) }); valid = false; }
			}
			void Select(int index, bool restore = true)
			{
				selected = index;
				tankName.Text = roster.Designs[selected].Name;
				space?.RetainDesigns(roster.Designs);
				space?.SelectDesign(roster.Designs[selected], restore);
				Refresh(false);
			}
			factionName.OnTextEdited = () => Refresh(true);
			tankName.OnTextEdited = () => Refresh(true);
			space = new CustomVehicleSpaceLogic(widget, modData, compiler, Profile, (role, id) =>
			{
				var design = roster.Designs[selected];
				var before = new Dictionary<string, string>(design.Parts);
				try
				{
					compiler.SelectPart(Profile(), role, id); compiler.ValidateRoster(roster); Refresh(true); return true;
				}
				catch (Exception e) { design.Parts = before; Refresh(false); message = e.Message; return false; }
			}, text => message = text);
			space.SelectDesign(roster.Designs[selected], loadedRoster);
			void ConfirmPlanningExport(Action action)
			{
				if (!space.HasPlanningModules) { action(); return; }
				ConfirmationDialogs.ButtonPrompt(modData, "Planning-only modules",
					"Batteries, PDL and Reflector layout blocks are not saved or compiled yet. Continue with configured stock parts only?",
					onConfirm: action, confirmText: "Stock parts only", onCancel: () => { }, cancelText: "Cancel");
			}

			var basis = widget.Get<DropDownButtonWidget>("BASE");
			basis.GetText = () => CustomFactionDesign.BaseLabel(roster.BaseFaction);
			basis.IsDisabled = () => !valid;
			basis.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(string id, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => roster.BaseFaction == id, () =>
					{
						if (id == roster.BaseFaction) return;
						ConfirmationDialogs.ButtonPrompt(modData, "New base faction",
							"Save this faction to the library and start a new faction?",
							onConfirm: () =>
							{
								try
								{
									compiler.SaveLibrary(libraryPath, roster);
									var next = compiler.NewRoster(id, libraryPath);
									roster = next; factionName.Text = roster.Name; Select(0); Refresh(true);
									message = "Previous faction saved. New base selected; add templates for compatible vehicles.";
								}
								catch (Exception e) { message = "Base faction switch failed: " + e.Message; }
							}, confirmText: "Save and switch", onCancel: () => { }, cancelText: "Cancel");
					});
					item.Get<LabelWidget>("LABEL").GetText = () => CustomFactionDesign.BaseLabel(id);
					return item;
				}
				basis.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", CustomFactionDesign.BaseFactions.Length * 30, CustomFactionDesign.BaseFactions, Setup);
			};

			var designs = widget.Get<DropDownButtonWidget>("DESIGN");
			designs.GetText = () => $"{selected + 1}/{roster.Designs.Count}  {roster.Designs[selected].Name}";
			designs.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(int index, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => selected == index, () => Select(index));
					item.Get<LabelWidget>("LABEL").GetText = () => (index + 1) + ". " + roster.Designs[index].Name;
					return item;
				}
				designs.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(roster.Designs.Count, 8) * 30,
					Enumerable.Range(0, roster.Designs.Count), Setup);
			};
			foreach (var id in new[] { "ADD", "COPY" })
			{
				var button = widget.Get<ButtonWidget>(id);
				button.IsDisabled = () => !valid || roster.Designs.Count >= CustomFactionDesign.MaxDesigns;
				button.OnClick = () =>
				{
					try
					{
						compiler.AddDesign(roster, id == "COPY" ? roster.Designs[selected] : null);
						Select(roster.Designs.Count - 1, id == "COPY"); Refresh(true);
					}
					catch (Exception e) { message = e.Message; }
				};
			}
			var delete = widget.Get<ButtonWidget>("DELETE");
			delete.IsDisabled = () => roster.Designs.Count <= 1;
			delete.OnClick = () => ConfirmationDialogs.ButtonPrompt(modData, "Remove design", "Remove " + roster.Designs[selected].Name + " from this faction?",
				onConfirm: () => { roster.Designs.RemoveAt(selected); Select(Math.Min(selected, roster.Designs.Count - 1)); Refresh(true); },
				confirmText: "Remove", onCancel: () => { }, cancelText: "Cancel");
			var templates = widget.Get<ButtonWidget>("TEMPLATES");
			templates.IsDisabled = () => !valid;
			templates.OnClick = () =>
			{
				var before = compiler.SerializeRoster(roster);
				try
				{
					var remaining = compiler.AddTemplates(roster);
					Select(selected); Refresh(true);
					message = remaining == 0 ? "All templates for this base have been added." :
						remaining + " more chassis available; faction limit is 16 designs / 50 points.";
				}
				catch (Exception e) { roster = compiler.DeserializeRoster(before); Select(selected); message = e.Message; }
			};

			var load = widget.Get<DropDownButtonWidget>("LOAD");
			load.GetText = () => "Load faction...";
			load.OnMouseDown = _ =>
			{
				try
				{
					var files = Directory.Exists(libraryPath) ? Directory.GetFiles(libraryPath, "faction-*.json").OrderBy(f => f, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
					var entries = new System.Collections.Generic.List<(string Name, string Path)>();
					foreach (var path in files)
					{
						try { entries.Add((compiler.DeserializeRoster(File.ReadAllText(path)).Name, path)); }
						catch (Exception e) { message = "A saved profile could not be read: " + e.Message; }
					}
					if (entries.Count == 0) { message = "No readable saved factions. Save a faction first."; return; }
					ScrollItemWidget Setup((string Name, string Path) entry, ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(template, () => false, () => DiscardThen(() =>
						{
							try
							{
								roster = compiler.DeserializeRoster(File.ReadAllText(entry.Path));
								factionName.Text = roster.Name; dirty = false; Select(0); message = "Faction loaded.";
							}
							catch (Exception e) { message = "Load failed: " + e.Message; }
						}));
						item.Get<LabelWidget>("LABEL").GetText = () => entry.Name;
						return item;
					}
					load.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(entries.Count, 8) * 30, entries.OrderBy(e => e.Name), Setup);
				}
				catch (Exception e) { message = "Library could not be loaded: " + e.Message; }
			};
			void Save()
			{
				compiler.SaveLibrary(libraryPath, roster);
				compiler.SaveRoster(profilePath, roster);
				dirty = false;
			}
			Refresh(false);
			var save = widget.Get<ButtonWidget>("SAVE");
			save.IsDisabled = () => !valid || !space.Ready;
			save.OnClick = () => ConfirmPlanningExport(() =>
			{
				try { Save(); message = "Faction and configured parts saved. Previous version retained as .bak; grid positions are temporary."; }
				catch (Exception e) { message = "Save failed: " + e.Message; }
			});
			var play = widget.Get<ButtonWidget>("PLAY");
			play.IsDisabled = () => !valid || !space.Ready;
			play.OnClick = () => ConfirmPlanningExport(() =>
			{
				try
				{
					byte[] lab;
					using (var stream = modData.DefaultFileSystem.Open("ca|maps/modular-gdi-lab.oramap"))
					using (var memory = new MemoryStream()) { stream.CopyTo(memory); lab = memory.ToArray(); }
					var bytes = compiler.CompileRosterMap(roster, lab);
					var directory = Path.Combine(Platform.SupportDir, "maps", "ca", "modular");
					Directory.CreateDirectory(directory);
					var filename = "custom-" + CustomFactionDesign.ContentHash(bytes) + ".oramap";
					var path = Path.Combine(directory, filename);
					if (!File.Exists(path))
					{
						var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
						try { File.WriteAllBytes(temp, bytes); File.Move(temp, path); }
						finally { if (File.Exists(temp)) File.Delete(temp); }
					}
					else if (!File.ReadAllBytes(path).SequenceEqual(bytes))
						throw new InvalidDataException("Existing test map was modified; it will not be overwritten.");
					var location = modData.MapCache.MapLocations.Where(kv => kv.Value == MapClassification.User).Select(kv => kv.Key).First(p =>
						string.Equals(Path.GetFullPath(p.Name).TrimEnd(Path.DirectorySeparatorChar),
						Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
					string uid;
					using (var package = location.OpenPackage(filename, modData.ModFiles)) uid = Map.ComputeUID(package);
					modData.MapCache.LoadMap(filename, location, MapClassification.User, modData.Manifest.Get<MapGrid>(), null);
					if (modData.MapCache[uid].Status != MapStatus.Available) throw new InvalidDataException("The generated map could not be loaded.");
					Save();
					modData.MapCache.PickLastModifiedMap(MapVisibility.Lobby);
					Ui.CloseWindow(); onPlay(uid);
				}
				catch (Exception e) { message = "Test game failed: " + e.Message; }
			});
		}
	}
}
