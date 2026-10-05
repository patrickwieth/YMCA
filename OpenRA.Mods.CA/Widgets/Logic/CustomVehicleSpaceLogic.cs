using System;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

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
			var chassis = widget.Get<DropDownButtonWidget>("CHASSIS");
			var turret = widget.Get<DropDownButtonWidget>("TURRET");
			layout.LoadExample();
			var message = "Beispiel geladen: Rumpf und Turm getrennt bestuecken; goldener Dock waehlt den Turm.";
			widget.Get<ButtonWidget>("EXAMPLE").OnClick = () => { layout.LoadExample(); canvas.Cancel(); message = "Beispielbelegung wiederhergestellt."; };
			canvas.Notify = s => message = s;
			widget.Get<LabelWidget>("STATUS").GetText = () => WidgetUtils.TruncateText(
				(layout.HasPdlWithoutBattery ? "PDL ohne Akku! " : "") + message, 752, Game.Renderer.Fonts["Small"]);
			widget.Get<LabelWidget>("PREVIEW_STATUS").GetText = () => preview.Status;
			widget.Get<LabelWidget>("SELECTION").GetText = () => canvas.Selected == null ? "Bausteine: Waffe / PDL im Turm; Fahrwerk / Motor / Generator / Reflector im Rumpf; Akku / Munition in beiden." :
				"Ausgewaehlt: " + CustomVehicleSpaceDemo.Definition(canvas.Selected).Label + (canvas.Rotated ? " (gedreht)" : "") + " - gruene Markierung passt, rote nicht.";
			widget.Get<ButtonWidget>("BACK").OnClick = () => Ui.CloseWindow();
			widget.Get<ButtonWidget>("CANCEL").OnClick = canvas.Cancel;
			var rotate = widget.Get<ButtonWidget>("ROTATE");
			rotate.GetText = () => "Modul drehen (90 Grad)";
			rotate.IsDisabled = () => canvas.Selected == null;
			rotate.OnClick = () => canvas.Rotated = !canvas.Rotated;
			chassis.GetText = () => layout.Heavy ? "Chassis: Mammut - Rumpf 5 x 6" : "Chassis: Battle Tank - Rumpf 4 x 5";
			chassis.OnMouseDown = _ =>
			{
				ScrollItemWidget Setup(bool heavy, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => layout.Heavy == heavy, () =>
					{
						if (heavy == layout.Heavy) return;
						layout.SetChassis(heavy); canvas.Cancel();
						preview.SetVehicle(heavy ? "mammoth" : "mtnk", "eagle");
						message = "Chassis gewechselt; das temporaere Layout wurde geleert.";
					});
					item.Get<LabelWidget>("LABEL").GetText = () => heavy ? "Mammut (Beispiel: 5 x 6)" : "Battle Tank (Beispiel: 4 x 5)";
					return item;
				}
				chassis.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 60, new[] { false, true }, Setup);
			};
			string TurretLabel(int width) => width == 0 ? "Kein Turm" : $"Beispielturm {width} x 4";
			turret.GetText = () => "Dock: " + TurretLabel(layout.TurretWidth);
			void ChooseTurret()
			{
				ScrollItemWidget Setup(int width, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => layout.TurretWidth == width, () =>
					{
						if (width == layout.TurretWidth) return;
						layout.SetTurret(width); canvas.Cancel();
						message = "Turm gewechselt; Turminhalt geleert, Rumpfinhalt bleibt erhalten.";
					});
					item.Get<LabelWidget>("LABEL").GetText = () => TurretLabel(width);
					return item;
				}
				turret.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 90, new[] { 0, 3, 4 }, Setup);
			}
			turret.OnMouseDown = _ => ChooseTurret();
			canvas.Dock = ChooseTurret;
			var palette = widget.Get("PALETTE");
			for (var i = 0; i < CustomVehicleSpaceDemo.Modules.Count; i++)
			{
				var module = CustomVehicleSpaceDemo.Modules[i];
				palette.AddChild(new ButtonWidget(modData)
				{
					Bounds = new WidgetBounds(i % 4 * 188, i / 4 * 32, 180, 28),
					GetText = () => $"{module.Label} ({module.Width} x {module.Height})",
					OnClick = () => { canvas.Choose(module.Id); message = "Zielposition anklicken; der ganze Baustein muss hineinpassen."; }
				});
			}
			preview.SetVehicle("mtnk", "eagle");
		}
	}
}
