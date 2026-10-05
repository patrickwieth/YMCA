using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;
using Zone = OpenRA.Mods.CA.Modular.CustomVehicleSpaceDemo.Zone;

namespace OpenRA.Mods.CA.Widgets
{
	public sealed class CustomVehicleSpaceWidget : Widget
	{
		public CustomVehicleSpaceDemo Layout = new();
		public string Selected;
		public bool Rotated;
		public int? Moving;
		public Action<string> Notify = _ => { };
		public Func<string, bool> OnInstall = _ => true;
		public Action OnLayoutChanged = () => { };
		public Action OnSelectionCancelled = () => { };
		const int Cell = 28;
		static readonly int2 HullOrigin = new(72, 202);
		static readonly int2 TurretOrigin = new(156, 32);

		public void Cancel() { Selected = null; Moving = null; Rotated = false; OnSelectionCancelled(); }
		public void Choose(string id) { Cancel(); Selected = id; }
		public static Color ZoneColor(Zone zone) => zone switch
		{
			Zone.RunningGear => Color.FromArgb(255, 230, 193, 63),
			Zone.Weapon => Color.FromArgb(255, 231, 87, 80),
			Zone.Armor => Color.FromArgb(255, 76, 158, 235),
			Zone.Interior => Color.FromArgb(255, 86, 175, 111),
			Zone.Ammunition => Color.FromArgb(255, 178, 113, 224),
			_ => Color.FromArgb(255, 100, 123, 130)
		};
		static Color ZoneFill(Zone zone) => zone switch
		{
			Zone.Interior => Color.FromArgb(255, 22, 52, 36),
			Zone.Ammunition => Color.FromArgb(255, 48, 29, 66),
			Zone.Weapon => Color.FromArgb(255, 64, 29, 30),
			Zone.RunningGear => Color.FromArgb(255, 58, 50, 26),
			Zone.Armor => Color.FromArgb(255, 26, 43, 64),
			_ => Color.FromArgb(255, 25, 34, 41)
		};
		static Color Tint(string id) => id switch
		{
			"ammo" => ZoneColor(Zone.Ammunition),
			"pdl" => Color.FromArgb(255, 101, 169, 169),
			"battery" => Color.FromArgb(255, 101, 169, 169),
			"generator" => ZoneColor(Zone.Interior),
			_ => ZoneColor(CustomVehicleSpaceDemo.Definition(id).Mount)
		};

		void Fill(int x, int y, int w, int h, Color c) => WidgetUtils.FillRectWithColor(new Rectangle(RenderOrigin.X + x, RenderOrigin.Y + y, w, h), c);
		void Text(string s, int x, int y, Color c) => Game.Renderer.Fonts["Small"].DrawText(s, new float2(RenderOrigin.X + x, RenderOrigin.Y + y), c);
		void Ellipse(int x, int y, int w, int h, Color color) => Game.Renderer.RgbaColorRenderer.FillEllipse(
			new float3(RenderOrigin.X + x, RenderOrigin.Y + y, 0), new float3(RenderOrigin.X + x + w, RenderOrigin.Y + y + h, 0), color);
		void Polygon(Color color, params int2[] points)
		{
			float3 At(int2 p) => new float3(RenderOrigin.X + p.X, RenderOrigin.Y + p.Y, 0);
			for (var i = 1; i < points.Length - 1; i++) Game.Renderer.RgbaColorRenderer.FillTriangle(At(points[0]), At(points[i]), At(points[i + 1]), color);
		}

		void Border(int x, int y, int w, int h, Color c)
		{
			Fill(x, y, w, 2, c); Fill(x, y + h - 2, w, 2, c);
			Fill(x, y, 2, h, c); Fill(x + w - 2, y, 2, h, c);
		}

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

		public override bool HandleKeyPress(KeyInput input)
		{
			if (Selected == null || input.Event != KeyInputEvent.Down) return false;
			if (input.Key == Keycode.ESCAPE) { Cancel(); return true; }
			if (input.Key == Keycode.R) { Rotated = !Rotated; return true; }
			return false;
		}

		public override bool HandleMouseInput(MouseInput input)
		{
			var mouse = input.Location - RenderOrigin;
			if (!RenderBounds.Contains(input.Location)) return false;
			if (input.Event != MouseInputEvent.Up) return true;
			if (!HitGrid(mouse, out var turret, out var x, out var y))
			{
				if (input.Button == MouseButton.Right) Cancel();
				return true;
			}
			var occupant = Layout.At(turret, x, y);
			if (input.Button == MouseButton.Right)
			{
				if (Selected == null && occupant != null) { Layout.Remove(occupant.Id); OnLayoutChanged(); Notify("Layout block removed; configured stock part is unchanged."); }
				else { Cancel(); Notify("Selection cancelled. The original position is preserved."); }
				return true;
			}
			if (input.Button != MouseButton.Left) return true;
			if (Selected != null)
			{
				if (!Layout.CanPlace(Selected, turret, x, y, Rotated, Moving))
					Notify("Cannot place here: check the colored zone, free space and orientation.");
				else if (OnInstall(Selected))
				{
					Layout.Place(Selected, turret, x, y, Rotated, Moving);
					Cancel(); OnLayoutChanged(); Notify("Placed. Click a module to pick it up again.");
				}
			}
			else if (occupant != null)
			{
				Selected = occupant.ModuleId; Moving = occupant.Id; Rotated = occupant.Rotated;
				Notify("Module in hand. Click to drop; R to rotate; Esc to return it.");
			}
			return true;
		}

		void Item(string id, bool rotated, int x, int y, Color border, bool held)
		{
			var m = CustomVehicleSpaceDemo.Definition(id);
			var size = CustomVehicleSpaceDemo.Size(m, rotated);
			var w = size.Width * Cell - 2;
			var h = size.Height * Cell - 2;
			Fill(x, y, w, h, held ? Color.FromArgb(220, 35, 49, 56) : Color.FromArgb(255, 39, 55, 62));
			for (var iy = 0; iy < size.Height; iy++)
				for (var ix = 0; ix < size.Width; ix++) Border(x + ix * Cell, y + iy * Cell, Cell - 2, Cell - 2, Tint(id));
			Border(x, y, w, h, border);
			var label = WidgetUtils.TruncateText(m.Label, w - 6, Game.Renderer.Fonts["Small"]);
			Text(label, x + 3, y + 2, Color.White);
			if (held) Text($"{size.Width} x {size.Height}", x + 3, y + h - 13, Color.White);
		}

		// Drawn last by a non-intercepting window overlay, including above the sidebar.
		public void DrawHeld(Rectangle window)
		{
			if (Selected == null || !window.Contains(Viewport.LastMousePos)) return;
			var mouse = Viewport.LastMousePos - RenderOrigin;
			if (HitGrid(mouse, out var turret, out var x, out var y))
			{
				var o = turret ? TurretOrigin : HullOrigin;
				Item(Selected, Rotated, o.X + x * Cell, o.Y + y * Cell,
					Layout.CanPlace(Selected, turret, x, y, Rotated, Moving) ? Color.Lime : Color.Red, true);
			}
			else
			{
				var size = CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(Selected), Rotated);
				var px = Math.Clamp(Viewport.LastMousePos.X + 14, window.Left, window.Right - size.Width * Cell);
				var py = Math.Clamp(Viewport.LastMousePos.Y + 14, window.Top, window.Bottom - size.Height * Cell);
				Item(Selected, Rotated, px - RenderOrigin.X, py - RenderOrigin.Y, Tint(Selected), true);
			}
		}

		public override void Draw()
		{
			var dark = Color.FromArgb(255, 25, 34, 41);
			var metal = Color.FromArgb(255, 63, 78, 84);
			Fill(0, 0, Bounds.Width, Bounds.Height, dark);
			Text("LEFT SIDE VIEW", 16, 8, Color.White);
			Text("FRONT  -->", 382, 8, Color.White);
			if (!Layout.HasChassis) { Text("Choose a chassis on the left to begin.", 104, 180, Color.White); return; }
			var hw = Layout.HullWidth * Cell;
			var hh = Layout.HullHeight * Cell;
			// Independent tank contour: sloped glacis, rear deck, track loop and road wheels.
			var trackY = 202 + hh - 10;
			var track = Color.FromArgb(255, 17, 22, 25);
			Ellipse(26, trackY, 56, 46, track); Ellipse(hw + 76, trackY, 56, 46, track);
			Fill(54, trackY, hw + 50, 46, track);
			for (var x = 48; x < hw + 118; x += 36)
			{
				Ellipse(x, trackY + 5, 32, 32, metal);
				Ellipse(x + 10, trackY + 15, 12, 12, dark);
			}
			for (var x = 38; x < hw + 122; x += 15) Fill(x, trackY + 40, 10, 3, metal);
			Polygon(metal, new int2(26, 214), new int2(56, 183), new int2(hw + 76, 183),
				new int2(hw + 132, 224), new int2(hw + 110, 202 + hh), new int2(40, 202 + hh));
			Fill(38, 179, 83, 5, metal);
			for (var x = 45; x < 113; x += 12) Fill(x, 185, 6, 8, dark);
			if (Layout.TurretWidth > 0)
			{
				var tw = Layout.TurretWidth * Cell;
				var bottom = TurretOrigin.Y + Layout.TurretHeight * Cell + 6;
				Polygon(metal, new int2(128, bottom), new int2(132, 44), new int2(158, 18),
					new int2(tw + 146, 18), new int2(tw + 178, 51), new int2(tw + 180, bottom - 10), new int2(tw + 156, bottom + 6));
				Fill(170, 12, 36, 6, metal); Fill(145, 0, 3, 36, metal);
				Ellipse(169, bottom + 2, 86, 16, metal);
				foreach (var weapon in Layout.Placements.Where(p => p.InTurret && p.ModuleId == "weapon" && p.Id != Moving))
				{
					var size = CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(weapon.ModuleId), weapon.Rotated);
					var center = TurretOrigin.Y + weapon.Y * Cell + size.Height * Cell / 2;
					Fill(156 + tw + 8, center - 7, 90, 14, metal);
					Fill(156 + tw + 92, center - 11, 14, 22, ZoneColor(Zone.Weapon));
				}
				Fill(194, bottom + 12, 34, 188 - bottom - 12, metal);
				Text($"Turret {Layout.TurretWidth} x {Layout.TurretHeight}", 20, 48, Color.White);
				Text($"{Layout.Used(true)} cells used", 20, 65, Color.White);
			}
			foreach (var t in new[] { false, true })
			{
				var o = t ? TurretOrigin : HullOrigin;
				var w = t ? Layout.TurretWidth : Layout.HullWidth;
				var h = t ? Layout.TurretHeight : Layout.HullHeight;
				for (var y = 0; y < h; y++)
					for (var x = 0; x < w; x++)
					{
						Fill(o.X + x * Cell, o.Y + y * Cell, Cell - 2, Cell - 2, ZoneFill(Layout.ZoneAt(t, x, y)));
						Border(o.X + x * Cell, o.Y + y * Cell, Cell - 2, Cell - 2, ZoneColor(Layout.ZoneAt(t, x, y)));
					}
			}
			foreach (var p in Layout.Placements)
			{
				if (p.Id == Moving) continue;
				var o = p.InTurret ? TurretOrigin : HullOrigin;
				Item(p.ModuleId, p.Rotated, o.X + p.X * Cell, o.Y + p.Y * Cell, Tint(p.ModuleId), false);
			}
			Text($"Schematic hull: {Layout.HullWidth} x {Layout.HullHeight} / {Layout.Used(false)} cells used", 72, 389, Color.White);
		}
	}

	public sealed class CustomVehicleSpaceCursorWidget : Widget
	{
		public CustomVehicleSpaceWidget Canvas;
		public override bool EventBoundsContains(int2 location) => false;
		public override void Draw() => Canvas?.DrawHeld(RenderBounds);
	}
}
