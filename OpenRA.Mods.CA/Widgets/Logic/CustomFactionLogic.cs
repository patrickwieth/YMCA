using System;
using System.IO;
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
							Game.Settings.Server.Map = uid;
							widget.Get<ButtonWidget>("SKIRMISH_BUTTON").OnClick();
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
			string message = "GDI-Roster plus eigene Fahrzeuge. Vorlagen fuegt die verfuegbaren Fahrzeugfamilien hinzu.";
			widget.Get<LabelWidget>("STATUS").GetText = () => message;
			bool dirty = false;
			void DiscardThen(Action action)
			{
				if (!dirty) { action(); return; }
				ConfirmationDialogs.ButtonPrompt(modData, "Ungespeicherte Aenderungen", "Aenderungen an dieser Fraktion verwerfen?",
					onConfirm: action, confirmText: "Verwerfen", onCancel: () => { }, cancelText: "Abbrechen");
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
				message = "Bausteinkatalog nicht geladen: " + e.Message;
				foreach (var id in new[] { "SAVE", "PLAY", "ADD", "COPY", "DELETE", "TEMPLATES" })
					widget.Get<ButtonWidget>(id).Disabled = true;
				return;
			}

			var profilePath = Path.Combine(Platform.SupportDir, "Modular", "custom-faction.json");
			var libraryPath = Path.Combine(Platform.SupportDir, "Modular", "Factions");
			var roster = new CustomFactionRoster();
			try
			{
				if (File.Exists(profilePath)) roster = compiler.DeserializeRoster(File.ReadAllText(profilePath));
			}
			catch (Exception e) { message = "Profil nicht geladen; Standard angezeigt. Alte Datei bleibt bis Speichern erhalten. " + e.Message; }

			var selected = 0;
			var factionName = widget.Get<TextFieldWidget>("FACTION_NAME");
			var tankName = widget.Get<TextFieldWidget>("TANK_NAME");
			factionName.Text = roster.Name;
			tankName.Text = roster.Designs[selected].Name;
			CustomFactionProfile Profile() => compiler.Profile(roster, roster.Designs[selected]);
			string stats = "", power = "", warning = "", weapons = "";
			bool valid = false;
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
					stats = $"Preis {v.Cost:0}   HP {v.Hp:0}   Tempo {v.Speed}   CP 0   Tech {v.Tech}   Entwuerfe {budget}/50";
					power = $"Masse {v.Mass:0} kg   Elektrisch {v.Electric:0.##} kW   Antriebsreserve {v.Reserve:0.##} kW";
					weapons = compiler.WeaponSummary(p);
					warning = v.Stationary ? "Stationaer: nur vorplatziert + Carryall, nicht baubar. Fahrzeuggrafik ist Platzhalter." :
						p.Parts["running_gear"] == "prototype-hover" && !compiler.IsNativeHover(p) ? "Hover mit Platzhaltergrafik. Baubar ab Tech 2; eigene Fahrzeuge hinten im Fahrzeugmenue." :
						"Baubar ab Tech " + v.Tech + "; eigene Fahrzeuge hinten im Fahrzeugmenue. Rumpf und Waffengrafik sind gebunden.";
					valid = true;
					if (edited) message = "Ungespeichert. Alle " + roster.Designs.Count + " Entwuerfe werden gemeinsam ins Testspiel uebernommen.";
				}
				catch (Exception e) { stats = e.Message; power = warning = weapons = ""; valid = false; }
			}
			void Select(int index)
			{
				selected = index;
				tankName.Text = roster.Designs[selected].Name;
				Refresh(false);
			}
			factionName.OnTextEdited = () => Refresh(true);
			tankName.OnTextEdited = () => Refresh(true);
			widget.Get<LabelWidget>("STATS").GetText = () => stats;
			widget.Get<LabelWidget>("POWER").GetText = () => power;
			widget.Get<LabelWidget>("WARNING").GetText = () => warning;
			widget.Get<LabelWidget>("WEAPON_STATS").GetText = () => weapons;

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
			foreach (var role in CustomFactionDesign.Roles)
			{
				var choice = widget.Get<DropDownButtonWidget>(role.ToUpperInvariant());
				choice.GetText = () => compiler.Label(roster.Designs[selected].Parts[role]);
				choice.OnMouseDown = _ =>
				{
					var options = compiler.CompatibleOptions(Profile(), role);
					ScrollItemWidget Setup(string id, ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(template, () => roster.Designs[selected].Parts[role] == id,
							() =>
							{
								compiler.SelectPart(Profile(), role, id); Refresh(true);
								if (role == "chassis") message = "Einbaugruppe gewechselt: nicht passende Bausteine wurden durch kompatible ersetzt.";
							});
						item.Get<LabelWidget>("LABEL").GetText = () => compiler.Label(id);
						return item;
					}
					choice.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(options.Length, 7) * 30, options, Setup);
				};
			}

			foreach (var id in new[] { "ADD", "COPY" })
			{
				var button = widget.Get<ButtonWidget>(id);
				button.IsDisabled = () => !valid || roster.Designs.Count >= CustomFactionDesign.MaxDesigns;
				button.OnClick = () =>
				{
					try
					{
						compiler.AddDesign(roster, id == "COPY" ? roster.Designs[selected] : null);
						Select(roster.Designs.Count - 1); Refresh(true);
					}
					catch (Exception e) { message = e.Message; }
				};
			}
			var delete = widget.Get<ButtonWidget>("DELETE");
			delete.IsDisabled = () => roster.Designs.Count <= 1;
			delete.OnClick = () => ConfirmationDialogs.ButtonPrompt(modData, "Entwurf entfernen", roster.Designs[selected].Name + " aus der Fraktion entfernen?",
				onConfirm: () => { roster.Designs.RemoveAt(selected); Select(Math.Min(selected, roster.Designs.Count - 1)); Refresh(true); },
				confirmText: "Entfernen", onCancel: () => { }, cancelText: "Abbrechen");
			var templates = widget.Get<ButtonWidget>("TEMPLATES");
			templates.IsDisabled = () => !valid;
			templates.OnClick = () =>
			{
				var before = compiler.SerializeRoster(roster);
				try
				{
					foreach (var hull in compiler.Options("chassis"))
					{
						if (roster.Designs.Any(d => d.Parts["chassis"] == hull)) continue;
						var d = compiler.AddDesign(roster);
						compiler.SelectPart(compiler.Profile(roster, d), "chassis", hull);
						var name = "Eigen " + compiler.Label(hull).Split(new[] { " - " }, StringSplitOptions.None)[0];
						if (name.Length > 32) name = name.Substring(0, 32);
						if (!roster.Designs.Any(other => other != d && string.Equals(other.Name, name, StringComparison.OrdinalIgnoreCase))) d.Name = name;
					}
					compiler.ValidateRoster(roster); Select(selected); Refresh(true);
				}
				catch (Exception e) { roster = compiler.DeserializeRoster(before); Select(selected); message = e.Message; }
			};

			var load = widget.Get<DropDownButtonWidget>("LOAD");
			load.GetText = () => "Fraktion laden...";
			load.OnMouseDown = _ =>
			{
				try
				{
					var files = Directory.Exists(libraryPath) ? Directory.GetFiles(libraryPath, "faction-*.json").OrderBy(f => f, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
					var entries = new System.Collections.Generic.List<(string Name, string Path)>();
					foreach (var path in files)
					{
						try { entries.Add((compiler.DeserializeRoster(File.ReadAllText(path)).Name, path)); }
						catch (Exception e) { message = "Ein gespeichertes Profil konnte nicht gelesen werden: " + e.Message; }
					}
					if (entries.Count == 0) { message = "Keine lesbaren gespeicherten Fraktionen. Erst Speichern verwenden."; return; }
					ScrollItemWidget Setup((string Name, string Path) entry, ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(template, () => false, () => DiscardThen(() =>
						{
							try
							{
								roster = compiler.DeserializeRoster(File.ReadAllText(entry.Path));
								factionName.Text = roster.Name; dirty = false; Select(0); message = "Fraktion geladen.";
							}
							catch (Exception e) { message = "Laden fehlgeschlagen: " + e.Message; }
						}));
						item.Get<LabelWidget>("LABEL").GetText = () => entry.Name;
						return item;
					}
					load.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(entries.Count, 8) * 30, entries.OrderBy(e => e.Name), Setup);
				}
				catch (Exception e) { message = "Bibliothek nicht geladen: " + e.Message; }
			};
			void Save()
			{
				compiler.SaveLibrary(libraryPath, roster);
				compiler.SaveRoster(profilePath, roster);
				dirty = false;
			}
			Refresh(false);
			var save = widget.Get<ButtonWidget>("SAVE");
			save.IsDisabled = () => !valid;
			save.OnClick = () =>
			{
				try { Save(); message = "Fraktion samt allen Entwuerfen gespeichert. Vorheriger Stand bleibt als .bak."; }
				catch (Exception e) { message = "Speichern fehlgeschlagen: " + e.Message; }
			};
			var play = widget.Get<ButtonWidget>("PLAY");
			play.IsDisabled = () => !valid;
			play.OnClick = () =>
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
						throw new InvalidDataException("Vorhandene Testkarte wurde veraendert; sie wird nicht ueberschrieben.");
					var location = modData.MapCache.MapLocations.Where(kv => kv.Value == MapClassification.User).Select(kv => kv.Key).First(p =>
						string.Equals(Path.GetFullPath(p.Name).TrimEnd(Path.DirectorySeparatorChar),
						Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
					string uid;
					using (var package = location.OpenPackage(filename, modData.ModFiles)) uid = Map.ComputeUID(package);
					modData.MapCache.LoadMap(filename, location, MapClassification.User, modData.Manifest.Get<MapGrid>(), null);
					if (modData.MapCache[uid].Status != MapStatus.Available) throw new InvalidDataException("Die erzeugte Karte konnte nicht geladen werden.");
					Save();
					modData.MapCache.PickLastModifiedMap(MapVisibility.Lobby);
					Ui.CloseWindow(); onPlay(uid);
				}
				catch (Exception e) { message = "Testspiel fehlgeschlagen: " + e.Message; }
			};
		}
	}
}
