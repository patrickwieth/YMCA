using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;
using Zone = OpenRA.Mods.CA.Modular.CustomVehicleSpaceDemo.Zone;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	// Embedded controller for the one faction designer, not a second window.
	public sealed class CustomVehicleSpaceLogic
	{
		readonly CustomVehicleSpaceWidget canvas;
		readonly CustomFactionDesign compiler;
		readonly Func<CustomFactionProfile> profile;
		readonly Dictionary<CustomVehicleDesign, CustomVehicleSpaceDemo> layouts = new();
		string pendingRole, pendingPart;
		public bool HasChassis => canvas.Layout.HasChassis;
		public bool Ready => canvas.Layout.HasChassis && canvas.Layout.TurretWidth > 0;
		public bool HasPlanningModules => layouts.Values.Any(l => l.Placements.Any(p => p.ModuleId is "battery" or "pdl" or "reflector"));

		public CustomVehicleSpaceLogic(Widget widget, ModData modData, CustomFactionDesign compiler,
			Func<CustomFactionProfile> profile, Func<string, string, bool> commitPart, Action<string> notify)
		{
			this.compiler = compiler; this.profile = profile;
			canvas = widget.Get<CustomVehicleSpaceWidget>("LAYOUT");
			var sidebar = widget.Get("SIDEBAR");
			canvas.Notify = notify;
			canvas.OnSelectionCancelled = () => { pendingRole = pendingPart = null; };
			canvas.OnInstall = _ => pendingRole == null || commitPart(pendingRole, pendingPart);
			var selection = widget.Get<LabelWidget>("SELECTION");
			selection.GetText = () => !Ready ? "Choose a chassis, then a turret / mount." : canvas.Selected == null ?
				"Pick a part on the left, then drop it into the grid." :
				$"In hand: {canvas.Layout.ModuleFor(canvas.Selected).Label} " +
				$"({CustomVehicleSpaceDemo.Size(canvas.Layout.ModuleFor(canvas.Selected), canvas.Rotated).Width} x " +
				$"{CustomVehicleSpaceDemo.Size(canvas.Layout.ModuleFor(canvas.Selected), canvas.Rotated).Height}) - R rotates; Esc cancels.";

			DropDownButtonWidget Field(string title, int row, Color color, Func<bool> visible)
			{
				sidebar.AddChild(new LabelWidget(modData)
				{
					Bounds = new WidgetBounds(0, row * 44, 220, 18), Font = "Small", GetText = () => title,
					GetColor = () => color, IsVisible = visible
				});
				var button = new DropDownButtonWidget(modData)
				{
					Bounds = new WidgetBounds(0, row * 44 + 19, 220, 24), GetColor = () => color, IsVisible = visible
				};
				sidebar.AddChild(button); return button;
			}
			string Fit(string text, int width = 188) => WidgetUtils.TruncateText(text, width, Game.Renderer.Fonts["Bold"]);
			void ButtonIcon(DropDownButtonWidget button, Func<string> module)
			{
				button.Align = TextAlign.Left; button.LeftMargin = 42;
				button.AddChild(new CustomVehicleModuleIconWidget { Bounds = new WidgetBounds(5, 0, 32, button.Bounds.Height), GetModule = module });
			}
			void OptionIcon(ScrollItemWidget item, string module)
			{
				var label = item.Get<LabelWidget>("LABEL");
				var x = label.Bounds.X;
				label.Bounds.X += 36; label.Bounds.Width -= 36;
				var text = label.GetText;
				label.GetText = () => WidgetUtils.TruncateText(text(), label.Bounds.Width, Game.Renderer.Fonts[label.Font]);
				item.AddChild(new CustomVehicleModuleIconWidget { Bounds = new WidgetBounds(x, (item.Bounds.Height - 24) / 2, 32, 24), GetModule = () => module });
			}
			var chassis = Field("Chassis", 0, Color.White, () => true);
			chassis.GetText = () => canvas.Layout.HasChassis ? Fit(compiler.Label(profile().Parts["chassis"])) : "Select chassis...";
			chassis.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(string id, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => canvas.Layout.HasChassis && profile().Parts["chassis"] == id, () =>
					{
						if (canvas.Layout.HasChassis && profile().Parts["chassis"] == id) return;
						if (!commitPart("chassis", id)) return;
						canvas.Cancel(); pendingRole = pendingPart = null;
						canvas.Layout.Silhouette = Silhouette();
						canvas.Layout.WeaponKind = WeaponKind();
						canvas.Layout.SetChassis(LargeSchematic());
						notify("Chassis selected. Choose its compatible turret / mount next.");
					});
					item.Get<LabelWidget>("LABEL").GetText = () => compiler.Label(id); return item;
				}
				var options = compiler.CompatibleOptions(profile(), "chassis");
				chassis.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(options.Length, 8) * 30, options, Setup);
			};
			var turret = Field("Turret", 1, Color.White, () => canvas.Layout.HasChassis);
			turret.GetText = () => Ready ? Fit(compiler.Label(profile().Parts["carrier"])) : "Select turret / mount...";
			void ChooseTurret()
			{
				if (!canvas.Layout.HasChassis) return;
				ScrollItemWidget Setup(string id, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => Ready && profile().Parts["carrier"] == id, () =>
					{
						if (!commitPart("carrier", id)) return;
						canvas.Cancel(); pendingRole = pendingPart = null;
						canvas.Layout.WeaponKind = WeaponKind();
						if (!Ready) canvas.Layout.LoadStockConfiguration(canvas.Layout.Heavy);
						else canvas.Layout.SetTurret(5);
						notify("Stock parts arranged. Pick up a replacement from the sidebar or move a block.");
					});
					item.Get<LabelWidget>("LABEL").GetText = () => compiler.Label(id); return item;
				}
				var options = compiler.CompatibleOptions(profile(), "carrier");
				turret.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", Math.Min(options.Length, 8) * 30, options, Setup);
			}
			turret.OnMouseDown = _ => ChooseTurret();

			void Pick(string module, string role, string part)
			{
				canvas.Choose(module); pendingRole = role; pendingPart = part;
				if (role != null)
				{
					// The compiler has one component per native role. Picking a replacement moves that block.
					var existing = canvas.Layout.Placements.FirstOrDefault(p => p.ModuleId == module);
					if (existing != null) { canvas.Moving = existing.Id; canvas.Rotated = existing.Rotated; }
				}
				notify(role == null ? "Planning-only module: not saved or included in test games." : "Drop to apply this stock part and update preview / stats.");
			}
			void PartField(string title, int row, Color color, string role, string module, bool armor = false)
			{
				var button = Field(title, row, color, () => Ready);
				ButtonIcon(button, () => module);
				button.GetText = () => Fit(compiler.Label(profile().Parts[role]), 150);
				button.OnMouseDown = _ =>
				{
					ScrollItemWidget Setup(string id, ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(template, () => profile().Parts[role] == id, () =>
						{
							if (id == "@reflector") Pick("reflector", null, null); else Pick(module, role, id);
						});
						item.Get<LabelWidget>("LABEL").GetText = () => id == "@reflector" ? "Reflector armor (planning)" : compiler.Label(id);
						item.Get<LabelWidget>("LABEL").GetColor = () => color;
						OptionIcon(item, id == "@reflector" ? "reflector" : module); return item;
					}
					var options = compiler.CompatibleOptions(profile(), role).Concat(armor ? new[] { "@reflector" } : Array.Empty<string>()).ToArray();
					button.ShowDropDown("MODULAR_PART_DROPDOWN_TEMPLATE", Math.Min(options.Length, 7) * 32, options, Setup);
				};
			}
			PartField("Engine", 2, CustomVehicleSpaceWidget.ZoneColor(Zone.Interior), "drive", "engine");
			PartField("Generator", 3, CustomVehicleSpaceWidget.ZoneColor(Zone.Interior), "generator", "generator");
			PartField("Running gear", 4, CustomVehicleSpaceWidget.ZoneColor(Zone.RunningGear), "running_gear", "gear");
			PartField("Weapon", 5, CustomVehicleSpaceWidget.ZoneColor(Zone.Weapon), "weapon", "weapon");
			PartField("Ammunition", 6, CustomVehicleSpaceWidget.ZoneColor(Zone.Ammunition), "ammunition", "ammo");
			PartField("Armor", 7, CustomVehicleSpaceWidget.ZoneColor(Zone.Armor), "armor", "armor", true);
			var free = Field("Free modules", 8, Color.White, () => Ready);
			free.GetText = () => "Pick up...";
			ButtonIcon(free, () => canvas.Selected is "pdl" or "battery" ? canvas.Selected : "battery");
			free.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(string id, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => false, () => Pick(id, null, null));
					item.Get<LabelWidget>("LABEL").GetText = () => CustomVehicleSpaceDemo.Definition(id).Label + " (planning, any slots)";
					OptionIcon(item, id);
					return item;
				}
				var options = new[] { "battery", "pdl" };
				free.ShowDropDown("MODULAR_PART_DROPDOWN_TEMPLATE", Math.Min(options.Length, 7) * 32, options, Setup);
			};
			// New pickups use pending part IDs; moving an existing block without a sidebar choice changes layout only.
			canvas.OnLayoutChanged = () => { pendingRole = pendingPart = null; };
			widget.AddChild(new CustomVehicleSpaceCursorWidget { Bounds = new WidgetBounds(0, 0, widget.Bounds.Width, widget.Bounds.Height), Canvas = canvas });
		}

		CustomWeaponKind WeaponKind() => CustomWeaponSocket.ForCarrier(profile().Parts["carrier"]);

		CustomVehicleSilhouetteKind Silhouette() => CustomVehicleSilhouette.ForActor(
			compiler.PreviewActor(profile()), compiler.IsNativeHover(profile()));

		bool LargeSchematic()
		{
			// Editing a name must not break layout switching; geometry does not depend on names.
			var current = profile();
			return compiler.Calculate(new CustomFactionProfile
			{
				Name = "Layout", TankName = "Layout", BaseFaction = current.BaseFaction, Parts = current.Parts
			}).Mass >= 20000;
		}

		public void RetainDesigns(IEnumerable<CustomVehicleDesign> designs)
		{
			var keep = new HashSet<CustomVehicleDesign>(designs);
			foreach (var key in layouts.Keys.Where(k => !keep.Contains(k)).ToArray()) layouts.Remove(key);
		}

		public void SelectDesign(CustomVehicleDesign design, bool restore)
		{
			canvas.Cancel(); pendingRole = pendingPart = null;
			if (!layouts.TryGetValue(design, out var layout))
			{
				layouts.Add(design, layout = new CustomVehicleSpaceDemo { Silhouette = Silhouette(), WeaponKind = WeaponKind() });
				if (restore)
				{
					layout.LoadStockConfiguration(LargeSchematic());
				}
			}
			layout.Silhouette = Silhouette();
			layout.WeaponKind = WeaponKind();
			canvas.Layout = layout;
		}
	}
}
