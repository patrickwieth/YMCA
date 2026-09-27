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
			var status = widget.Get<LabelWidget>("STATUS");
			string message = "Erster Umfang: GDI/Eagle plus ein eigener Panzer. CP-Freischaltungen folgen spaeter.";
			status.GetText = () => message;
			widget.Get<ButtonWidget>("BACK").OnClick = () => { Ui.CloseWindow(); onExit(); };
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
				widget.Get<ButtonWidget>("SAVE").Disabled = true;
				widget.Get<ButtonWidget>("PLAY").Disabled = true;
				return;
			}

			var profilePath = Path.Combine(Platform.SupportDir, "Modular", "custom-faction.json");
			var profile = new CustomFactionProfile();
			try
			{
				if (File.Exists(profilePath))
					profile = compiler.Deserialize(File.ReadAllText(profilePath));
			}
			catch (Exception e)
			{
				message = "Profil nicht geladen. Standard angezeigt; Speichern sichert die alte Datei als .bak. " + e.Message;
			}

			var factionName = widget.Get<TextFieldWidget>("FACTION_NAME");
			var tankName = widget.Get<TextFieldWidget>("TANK_NAME");
			factionName.Text = profile.Name;
			tankName.Text = profile.TankName;
			string stats = "", power = "", warning = "";
			bool valid = false;
			void Refresh(bool edited)
			{
				profile.Name = factionName.Text;
				profile.TankName = tankName.Text;
				try
				{
					var v = compiler.Calculate(profile);
					stats = $"Preis {v.Cost:0}   HP {v.Hp:0}   Tempo {v.Speed}   CP 0   Tech {v.Tech}   Katalog {v.Points}/50";
					power = $"Masse {v.Mass:0} kg   Elektrisch {v.Electric:0.##} kW   Antriebsreserve {v.Reserve:0.##} kW";
					warning = v.Stationary ? "Stationaer: Startaufstellung + Carryall, noch keine Fabrikproduktion. Grafik ist Platzhalter." :
						profile.Parts["running_gear"] == "prototype-hover" ? "Hover: Wasser-/EMP-Regeln aktiv; Panzer-Grafik ist noch Platzhalter." :
						"Normaler GDI-Roster bleibt erhalten. Dein Panzer kommt als zusaetzliche Fabrikoption dazu.";
					valid = true;
					if (edited)
						message = "Ungespeicherte Aenderungen. Testspiel friert diesen Entwurf in einer eigenen Karte ein.";
				}
				catch (Exception e)
				{
					stats = "Ungueltiger Entwurf: " + e.Message;
					power = warning = "";
					valid = false;
				}
			}

			factionName.OnTextEdited = () => Refresh(true);
			tankName.OnTextEdited = () => Refresh(true);
			widget.Get<LabelWidget>("STATS").GetText = () => stats;
			widget.Get<LabelWidget>("POWER").GetText = () => power;
			widget.Get<LabelWidget>("WARNING").GetText = () => warning;
			foreach (var role in CustomFactionDesign.Roles)
			{
				var choice = widget.Get<DropDownButtonWidget>(role.ToUpperInvariant());
				choice.GetText = () => compiler.Label(profile.Parts[role]);
				var options = compiler.Options(role);
				choice.OnMouseDown = _ =>
				{
					ScrollItemWidget Setup(string id, ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(template, () => profile.Parts[role] == id,
							() => { profile.Parts[role] = id; Refresh(true); });
						item.Get<LabelWidget>("LABEL").GetText = () => compiler.Label(id);
						return item;
					}

					choice.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Length * 30, options, Setup);
				};
			}

			Refresh(false);
			var save = widget.Get<ButtonWidget>("SAVE");
			save.IsDisabled = () => !valid;
			save.OnClick = () =>
			{
				try { compiler.Save(profilePath, profile); message = "Fraktion gespeichert (vorheriges Profil als .bak)."; }
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
					var bytes = compiler.CompileMap(profile, lab);
					var directory = Path.Combine(Platform.SupportDir, "maps", "ca", "modular");
					Directory.CreateDirectory(directory);
					var filename = "custom-" + CustomFactionDesign.ContentHash(bytes) + ".oramap";
					var path = Path.Combine(directory, filename);
					if (!File.Exists(path))
					{
						var temp = path + ".tmp";
						try { File.WriteAllBytes(temp, bytes); File.Move(temp, path); }
						finally { if (File.Exists(temp)) File.Delete(temp); }
					}
					else if (!File.ReadAllBytes(path).SequenceEqual(bytes))
						throw new InvalidDataException("Vorhandene Testkarte wurde veraendert; sie wird nicht ueberschrieben.");

					var location = modData.MapCache.MapLocations.Where(kv => kv.Value == MapClassification.User).Select(kv => kv.Key).First(p =>
						string.Equals(Path.GetFullPath(p.Name).TrimEnd(Path.DirectorySeparatorChar),
						Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
					string uid;
					using (var package = location.OpenPackage(filename, modData.ModFiles))
						uid = Map.ComputeUID(package);
					modData.MapCache.LoadMap(filename, location, MapClassification.User, modData.Manifest.Get<MapGrid>(), null);
					if (modData.MapCache[uid].Status != MapStatus.Available)
						throw new InvalidDataException("Die erzeugte Karte konnte nicht geladen werden.");
					compiler.Save(profilePath, profile);
					// Consume pending directory changes so the standard menu's last-modified heuristic
					// does not replace the explicitly selected snapshot on opening the lobby.
					modData.MapCache.PickLastModifiedMap(MapVisibility.Lobby);
					Ui.CloseWindow();
					onPlay(uid);
				}
				catch (Exception e) { message = "Testspiel fehlgeschlagen: " + e.Message; }
			};
		}
	}
}
