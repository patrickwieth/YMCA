using System;
using System.Linq;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;
using Zone = OpenRA.Mods.CA.Modular.CustomVehicleSpaceDemo.Zone;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public sealed class CustomVehicleSpaceLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public CustomVehicleSpaceLogic(Widget widget, ModData modData)
		{
			var canvas = widget.Get<CustomVehicleSpaceWidget>("LAYOUT");
			var layout = canvas.Layout;
			var preview = widget.Get<CustomVehiclePreviewWidget>("REFERENCE");
			preview.IsVisible = () => layout.HasChassis;
			widget.Get<LabelWidget>("PREVIEW_LABEL").IsVisible = () => layout.HasChassis;
			var sidebar = widget.Get("SIDEBAR");
			var message = "Choose a chassis, then mount a turret from above.";
			canvas.Notify = s => message = s;
			widget.Get<LabelWidget>("STATUS").GetText = () => WidgetUtils.TruncateText(
				(layout.HasPdlWithoutBattery ? "PDL needs a battery! " : "") + message, 554, Game.Renderer.Fonts["Small"]);
			widget.Get<LabelWidget>("SELECTION").GetText = () => canvas.Selected == null ? "Choose a module on the left to pick it up." :
				$"In hand: {CustomVehicleSpaceDemo.Definition(canvas.Selected).Label} - " +
				$"{CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(canvas.Selected), canvas.Rotated).Width} x " +
				$"{CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(canvas.Selected), canvas.Rotated).Height}. Green fits; red cannot be placed.";
			widget.Get<ButtonWidget>("BACK").OnClick = () => Ui.CloseWindow();
			widget.Get<ButtonWidget>("CANCEL").OnClick = canvas.Cancel;
			var rotate = widget.Get<ButtonWidget>("ROTATE");
			rotate.IsDisabled = () => canvas.Selected == null;
			rotate.OnClick = () => canvas.Rotated = !canvas.Rotated;

			DropDownButtonWidget Field(string title, int row, Color color)
			{
				sidebar.AddChild(new LabelWidget(modData)
				{
					Bounds = new WidgetBounds(0, row * 58, 220, 18), Font = "Small", GetText = () => title, GetColor = () => color
				});
				var button = new DropDownButtonWidget(modData) { Bounds = new WidgetBounds(0, row * 58 + 20, 220, 28), GetColor = () => color };
				sidebar.AddChild(button);
				return button;
			}

			var chassis = Field("Chassis", 0, Color.White);
			chassis.GetText = () => !layout.HasChassis ? "Select chassis..." : layout.Heavy ? "Mammoth (10 x 5)" : "Battle Tank (8 x 4)";
			chassis.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(bool heavy, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => layout.HasChassis && layout.Heavy == heavy, () =>
					{
						if (layout.HasChassis && heavy == layout.Heavy) return;
						layout.SetChassis(heavy); canvas.Cancel();
						preview.SetVehicle(heavy ? "mammoth" : "mtnk", "eagle");
						message = "Chassis selected. Temporary layout cleared; choose a turret next.";
					});
					item.Get<LabelWidget>("LABEL").GetText = () => heavy ? "Mammoth (10 x 5)" : "Battle Tank (8 x 4)";
					return item;
				}
				chassis.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 60, new[] { false, true }, Setup);
			};

			var turret = Field("Turret", 1, Color.White);
			turret.IsDisabled = () => !layout.HasChassis;
			string TurretLabel(int width) => width == 0 ? "No turret" : $"Example turret ({width} x 4)";
			turret.GetText = () => !layout.HasChassis ? "Choose a chassis first" : TurretLabel(layout.TurretWidth);
			void ChooseTurret()
			{
				if (!layout.HasChassis) return;
				ScrollItemWidget Setup(int width, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => layout.TurretWidth == width, () =>
					{
						if (width == layout.TurretWidth) return;
						layout.SetTurret(width); canvas.Cancel();
						message = "Turret changed. Turret contents cleared; hull contents preserved.";
					});
					item.Get<LabelWidget>("LABEL").GetText = () => TurretLabel(width);
					return item;
				}
				turret.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 90, new[] { 0, 3, 4 }, Setup);
			}
			turret.OnMouseDown = _ => ChooseTurret();
			canvas.Dock = ChooseTurret;

			void ModuleField(string title, int row, Color color, params string[] ids)
			{
				var button = Field(title, row, color);
				button.IsDisabled = () => !layout.HasChassis || (ids.All(id => !CustomVehicleSpaceDemo.Definition(id).Hull) && layout.TurretWidth == 0);
				button.GetText = () => canvas.Selected != null && ids.Contains(canvas.Selected) ?
					CustomVehicleSpaceDemo.Definition(canvas.Selected).Label + " (in hand)" : "Pick up...";
				button.OnMouseDown = _ =>
				{
					ScrollItemWidget Setup(string id, ScrollItemWidget template)
					{
						var module = CustomVehicleSpaceDemo.Definition(id);
						var item = ScrollItemWidget.Setup(template, () => canvas.Selected == id, () =>
						{
							canvas.Choose(id); message = "Module in hand. Click a slot to drop it; R rotates, Esc cancels.";
						});
						item.Get<LabelWidget>("LABEL").GetText = () => $"{module.Label} ({module.Width} x {module.Height})";
						item.Get<LabelWidget>("LABEL").GetColor = () => color;
						return item;
					}
					var options = ids.Where(id => layout.TurretWidth > 0 || CustomVehicleSpaceDemo.Definition(id).Hull).ToArray();
					button.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Length * 30, options, Setup);
				};
			}
			ModuleField("Engine", 2, Color.White, "engine");
			ModuleField("Running gear - bottom slots", 3, CustomVehicleSpaceWidget.ZoneColor(Zone.RunningGear), "gear");
			ModuleField("Weapon - front turret dock", 4, CustomVehicleSpaceWidget.ZoneColor(Zone.Weapon), "weapon");
			ModuleField("Ammunition", 5, Color.White, "ammo");
			ModuleField("Armor - hull edge", 6, CustomVehicleSpaceWidget.ZoneColor(Zone.Armor), "armor", "reflector");
			ModuleField("Free modules", 7, Color.White, "generator", "battery", "pdl");
			widget.Get<ButtonWidget>("EXAMPLE").OnClick = () =>
			{
				layout.LoadExample(); canvas.Cancel(); preview.SetVehicle(layout.Heavy ? "mammoth" : "mtnk", "eagle");
				message = "Example loaded. Pick up a fitted module and try moving it.";
			};
			widget.AddChild(new CustomVehicleSpaceCursorWidget { Bounds = new WidgetBounds(0, 0, 800, 640), Canvas = canvas });
		}
	}
}
