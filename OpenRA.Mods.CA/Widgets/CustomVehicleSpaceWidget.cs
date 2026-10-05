using System;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public sealed class CustomVehicleSpaceWidget : Widget
	{
		public CustomVehicleSpaceDemo Layout = new();
		public string Selected;
		public bool Rotated;
		public int? Moving;
		public Action Dock = () => { };
		public Action<string> Notify = _ => { };
		int2 mouse = new(-1, -1);
		const int Cell = 28;
		static readonly int2 HullOrigin = new(52, 142);
		static readonly int2 TurretOrigin = new(306, 114);

		public void Cancel() { Selected = null; Moving = null; Rotated = false; }
		public void Choose(string id) { Cancel(); Selected = id; }
		static Color Tint(string id) => id switch
		{
			"weapon" => Color.FromArgb(255, 170, 75, 65),
			"ammo" => Color.FromArgb(255, 160, 120, 45),
			"pdl" => Color.FromArgb(255, 65, 130, 180),
			"battery" => Color.FromArgb(255, 75, 140, 85),
			"generator" => Color.FromArgb(255, 115, 90, 155),
			_ => Color.FromArgb(255, 90, 110, 120)
		};

		bool HitGrid(int2 point, out bool turret, out int x, out int y)
		{
			foreach (var t in new[] { false, true })
			{
				var o = t ? TurretOrigin : HullOrigin;
				var w = t ? Layout.TurretWidth : Layout.HullWidth;
				var h = t ? Layout.TurretHeight : Layout.HullHeight;
				if (new Rectangle(o.X, o.Y, w * Cell, h * Cell).Contains(point))
				{ turret = t; x = (point.X - o.X) / Cell; y = (point.Y - o.Y) / Cell; return true; }
			}
			turret = false; x = y = 0; return false;
		}

		public override bool HandleMouseInput(MouseInput input)
		{
			mouse = input.Location - RenderOrigin;
			if (!RenderBounds.Contains(input.Location)) return false;
			if (input.Event != MouseInputEvent.Up) return true;
			if (input.Button == MouseButton.Left && new Rectangle(91, 68, 70, 42).Contains(mouse)) { Dock(); return true; }
			if (!HitGrid(mouse, out var turret, out var x, out var y)) return true;
			var occupant = Layout.At(turret, x, y);
			if (input.Button == MouseButton.Right)
			{
				if (occupant != null) Layout.Remove(occupant.Id);
				Cancel(); Notify("Modul entfernt / Auswahl aufgehoben."); return true;
			}
			if (input.Button != MouseButton.Left) return true;
			if (Selected != null)
			{
				if (Layout.Place(Selected, turret, x, y, Rotated, Moving)) { Cancel(); Notify("Eingebaut. Modul anklicken zum Verschieben."); }
				else Notify("Passt hier nicht: Bauraum, Belegung oder Einbauort pruefen.");
			}
			else if (occupant != null)
			{
				Selected = occupant.ModuleId; Moving = occupant.Id; Rotated = occupant.Rotated;
				Notify("Neue Position anklicken. Abbrechen behaelt die alte Position.");
			}
			return true;
		}

		public override void Draw()
		{
			void Fill(int x, int y, int w, int h, Color c) => WidgetUtils.FillRectWithColor(new Rectangle(RenderOrigin.X + x, RenderOrigin.Y + y, w, h), c);
			void Text(string s, int x, int y, Color c) => Game.Renderer.Fonts["Small"].DrawText(s, new float2(RenderOrigin.X + x, RenderOrigin.Y + y), c);
			var dark = Color.FromArgb(255, 28, 38, 44);
			var metal = Color.FromArgb(255, 60, 75, 80);
			Fill(0, 0, Bounds.Width, Bounds.Height, dark);
			Text("RUMPF / CHASSIS", 32, 8, Color.White);
			Text("TURM / INNENRAUM", 292, 8, Color.White);
			var width = Layout.Heavy ? 180 : 152;
			Fill(32, 110, width, 211, metal); Fill(52, 50, width - 40, 60, metal);
			for (var i = 0; i < 12; i++)
			{
				Fill(12, 65 + i * 22, 17, 18, metal);
				Fill(35 + width, 65 + i * 22, 17, 18, metal);
			}
			Fill(91, 68, 70, 42, Color.FromArgb(255, 155, 120, 45));
			Text(Layout.TurretWidth == 0 ? "+ TURM" : "TURM", 102, 80, Color.White);
			Fill(164, 88, 111, 2, Color.Gold);
			if (Layout.TurretWidth > 0)
			{
				Fill(284, 92, 157, 153, metal); Fill(349, 37, 22, 55, metal);
				Text($"{Layout.TurretWidth} x 4", 336, 264, Color.White);
			}
			else Text("Dock anklicken: Turm waehlen", 264, 130, Color.Gold);
			foreach (var t in new[] { false, true })
			{
				var o = t ? TurretOrigin : HullOrigin;
				var w = t ? Layout.TurretWidth : Layout.HullWidth;
				var h = t ? Layout.TurretHeight : Layout.HullHeight;
				for (var y = 0; y < h; y++)
					for (var x = 0; x < w; x++) Fill(o.X + x * Cell, o.Y + y * Cell, Cell - 2, Cell - 2, dark);
				Text($"{Layout.Used(t)}/{w * h} Felder", t ? 312 : 62, 326, Color.White);
			}
			foreach (var p in Layout.Placements)
			{
				var o = p.InTurret ? TurretOrigin : HullOrigin;
				var m = CustomVehicleSpaceDemo.Definition(p.ModuleId);
				var size = CustomVehicleSpaceDemo.Size(m, p.Rotated);
				Fill(o.X + p.X * Cell, o.Y + p.Y * Cell, size.Width * Cell - 2, size.Height * Cell - 2, Tint(p.ModuleId));
				Text(m.Label.Substring(0, Math.Min(m.Label.Length, size.Width == 1 ? 3 : 7)), o.X + p.X * Cell + 2, o.Y + p.Y * Cell + 5, p.Id == Moving ? Color.Gold : Color.White);
			}
			if (Selected != null && HitGrid(mouse, out var target, out var gx, out var gy))
			{
				var o = target ? TurretOrigin : HullOrigin;
				var size = CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(Selected), Rotated);
				var valid = Layout.CanPlace(Selected, target, gx, gy, Rotated, Moving);
				for (var y = 0; y < size.Height; y++)
					for (var x = 0; x < size.Width; x++)
						if (gx + x < (target ? Layout.TurretWidth : Layout.HullWidth) && gy + y < (target ? Layout.TurretHeight : Layout.HullHeight))
							Fill(o.X + (gx + x) * Cell + 4, o.Y + (gy + y) * Cell + 4, 6, 6, valid ? Color.Lime : Color.Red);
			}
		}
	}
}
