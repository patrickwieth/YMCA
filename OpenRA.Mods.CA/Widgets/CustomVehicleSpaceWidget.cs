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
		int2 HullOrigin => Layout.Portrait ? new int2(176, 176) : new int2(72, 202);
		static readonly int2 TurretOrigin = new(156, 32);
		bool LightBody => Layout.Silhouette is CustomVehicleSilhouetteKind.LightVehicle or CustomVehicleSilhouetteKind.Bike or CustomVehicleSilhouetteKind.MiniTracked;
		bool DroneBody => Layout.Silhouette == CustomVehicleSilhouetteKind.MiniDrone;
		// Every container uses the same cell scale, including held-item snapping.
		int GridCell(bool turret) => Cell;

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
				var cell = GridCell(t);
				if (new Rectangle(o.X, o.Y, w * cell, h * cell).Contains(point))
				{ turret = t; x = (point.X - o.X) / cell; y = (point.Y - o.Y) / cell; return true; }
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

		void Item(string id, bool rotated, int x, int y, Color border, bool held, int cell = Cell)
		{
			var m = CustomVehicleSpaceDemo.Definition(id);
			var size = CustomVehicleSpaceDemo.Size(m, rotated);
			var w = size.Width * cell - 2;
			var h = size.Height * cell - 2;
			Fill(x, y, w, h, held ? Color.FromArgb(220, 35, 49, 56) : Color.FromArgb(255, 39, 55, 62));
			for (var iy = 0; iy < size.Height; iy++)
				for (var ix = 0; ix < size.Width; ix++) Border(x + ix * cell, y + iy * cell, cell - 2, cell - 2, Tint(id));
			Border(x, y, w, h, border);
			var iconWidth = w >= 80 && h < 40 ? 36 : w - 6;
			var hasIcon = CustomVehicleModuleIconWidget.DrawIcon(id,
				new Rectangle(RenderOrigin.X + x + 3, RenderOrigin.Y + y + 3, iconWidth, h - 6));
			if (!hasIcon || iconWidth == 36)
			{
				var offset = hasIcon ? 41 : 3;
				var label = WidgetUtils.TruncateText(m.Label, w - offset - 3, Game.Renderer.Fonts["Small"]);
				Text(label, x + offset, y + 2, Color.White);
			}
			if (held)
			{
				var dimensions = $"{size.Width}x{size.Height}";
				var width = Game.Renderer.Fonts["Small"].Measure(dimensions).X;
				Fill(x + w - width - 3, y + h - 14, width + 2, 13, Color.FromArgb(230, 13, 19, 25));
				Text(dimensions, x + w - width - 2, y + h - 13, Color.White);
			}
		}

		// Drawn last by a non-intercepting window overlay, including above the sidebar.
		public void DrawHeld(Rectangle window)
		{
			if (Selected == null || !window.Contains(Viewport.LastMousePos)) return;
			var mouse = Viewport.LastMousePos - RenderOrigin;
			if (HitGrid(mouse, out var turret, out var x, out var y))
			{
				var o = turret ? TurretOrigin : HullOrigin;
				var cell = GridCell(turret);
				Item(Selected, Rotated, o.X + x * cell, o.Y + y * cell,
					Layout.CanPlace(Selected, turret, x, y, Rotated, Moving) ? Color.Lime : Color.Red, true, cell);
			}
			else
			{
				var size = CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(Selected), Rotated);
				var px = Math.Clamp(Viewport.LastMousePos.X + 14, window.Left, window.Right - size.Width * Cell);
				var py = Math.Clamp(Viewport.LastMousePos.Y + 14, window.Top, window.Bottom - size.Height * Cell);
				Item(Selected, Rotated, px - RenderOrigin.X, py - RenderOrigin.Y, Tint(Selected), true);
			}
		}

		void DrawChassis(int hw, int hh, Color metal, Color dark)
		{
			var bottom = HullOrigin.Y + hh;
			var tire = Color.FromArgb(255, 17, 22, 25);
			void Wheel(int x, int diameter)
			{
				Ellipse(x, bottom - 20, diameter, diameter, tire);
				Ellipse(x + 8, bottom - 12, diameter - 16, diameter - 16, metal);
				Ellipse(x + diameter / 2 - 5, bottom + diameter / 2 - 25, 10, 10, dark);
			}
			switch (Layout.Silhouette)
			{
				case CustomVehicleSilhouetteKind.HeavyWalker:
					var left = HullOrigin.X;
					foreach (var x in new[] { left - 24, left + hw - 2 })
					{
						Polygon(metal, new int2(x, bottom - 80), new int2(x + 23, bottom - 76),
							new int2(x + 26, bottom - 25), new int2(x + 10, bottom + 10), new int2(x + 39, bottom + 13),
							new int2(x + 44, bottom + 25), new int2(x - 9, bottom + 25), new int2(x - 12, bottom + 10), new int2(x + 6, bottom - 25));
						Ellipse(x + 1, bottom - 36, 22, 22, dark);
						Fill(x - 7, bottom + 21, 46, 4, tire);
					}
					Polygon(metal, new int2(left - 16, 193), new int2(left - 4, 159), new int2(left + hw + 3, 159),
						new int2(left + hw + 18, 193), new int2(left + hw + 10, bottom - 14), new int2(left - 10, bottom - 14));
					Fill(left + 16, 163, hw - 30, 8, dark);
					break;
				case CustomVehicleSilhouetteKind.MiniDrone:
					Ellipse(51, 184, hw + 42, hh + 39, metal);
					Ellipse(42, 215, 25, 56, tire); Ellipse(hw + 76, 215, 25, 56, tire);
					Fill(49, 229, 11, 26, metal); Fill(hw + 83, 229, 11, 26, metal);
					Ellipse(hw + 91, 225, 16, 14, Color.FromArgb(255, 113, 176, 183));
					Fill(hw + 98, 250, 29, 5, metal);
					Ellipse(77, bottom + 31, hw - 6, 9, tire);
					Fill(92, bottom + 18, hw - 38, 2, Color.FromArgb(255, 64, 105, 111));
					break;
				case CustomVehicleSilhouetteKind.Walker:
				case CustomVehicleSilhouetteKind.Tripod:
					var hips = Layout.Silhouette == CustomVehicleSilhouetteKind.Tripod ? new[] { 48, 170, hw + 76 } : new[] { 48, hw + 76 };
					foreach (var x in hips)
					{
						Polygon(metal, new int2(x, bottom - 60), new int2(x + 25, bottom - 55),
							new int2(x + 36, bottom + 2), new int2(x + 13, bottom + 28), new int2(x + 46, bottom + 28),
							new int2(x + 52, bottom + 40), new int2(x - 10, bottom + 40), new int2(x - 10, bottom + 26), new int2(x + 10, bottom));
						Ellipse(x + 4, bottom - 10, 26, 26, dark); Ellipse(x + 11, bottom - 3, 12, 12, metal);
						Fill(x - 8, bottom + 36, 57, 4, tire);
					}
					Polygon(metal, new int2(30, 220), new int2(54, 180), new int2(hw + 98, 180),
						new int2(hw + 123, 221), new int2(hw + 105, bottom - 18), new int2(47, bottom - 18));
					Polygon(metal, new int2(92, 181), new int2(114, 153), new int2(170, 153), new int2(188, 181));
					Fill(115, 160, 48, 14, dark);
					break;
				case CustomVehicleSilhouetteKind.LightVehicle:
				case CustomVehicleSilhouetteKind.Wheeled:
				case CustomVehicleSilhouetteKind.Bike:
					Polygon(metal, new int2(34, 222), new int2(42, 194), new int2(184, 194),
						new int2(209, 180), new int2(245, 210), new int2(hw + 115, 220), new int2(hw + 121, bottom - 8), new int2(35, bottom - 8));
					if (Layout.Silhouette != CustomVehicleSilhouetteKind.Bike)
					{
						Polygon(metal, new int2(98, 198), new int2(106, 154), new int2(177, 154), new int2(201, 198));
						Polygon(dark, new int2(114, 163), new int2(172, 163), new int2(186, 189), new int2(110, 189));
						Fill(147, 160, 4, 34, metal);
					}
					else { Fill(95, 181, 66, 10, metal); Fill(hw + 48, 180, 7, 39, metal); }
					Wheel(43, 60); Wheel(hw + 55, 60);
					if (Layout.Silhouette == CustomVehicleSilhouetteKind.Wheeled) Wheel(166, 60);
					Fill(hw + 112, 230, 8, 16, Color.FromArgb(255, 142, 146, 116));
					break;
				case CustomVehicleSilhouetteKind.Hover:
					Polygon(metal, new int2(25, 240), new int2(69, 181), new int2(hw + 72, 181),
						new int2(hw + 135, 247), new int2(hw + 110, bottom), new int2(41, bottom));
					for (var x = 48; x < hw + 100; x += 84)
					{
						Ellipse(x, bottom - 8, 65, 24, tire); Fill(x + 10, bottom + 11, 45, 3, metal);
						Fill(x + 17, bottom + 26, 30, 2, Color.FromArgb(255, 64, 105, 111));
					}
					break;
				default:
					var trackY = bottom - 10;
					Ellipse(26, trackY, 56, 46, tire); Ellipse(hw + 76, trackY, 56, 46, tire);
					Fill(54, trackY, hw + 50, 46, tire);
					for (var x = 48; x < hw + 118; x += 36)
					{
						Ellipse(x, trackY + 5, 32, 32, metal); Ellipse(x + 10, trackY + 15, 12, 12, dark);
					}
					for (var x = 38; x < hw + 122; x += 15) Fill(x, trackY + 40, 10, 3, metal);
					Polygon(metal, new int2(26, 214), new int2(56, 183), new int2(hw + 76, 183),
						new int2(hw + 132, 224), new int2(hw + 110, bottom), new int2(40, bottom));
					Fill(38, 179, 83, 5, metal);
					for (var x = 45; x < 113; x += 12) Fill(x, 185, 6, 8, dark);
					break;
			}
		}

		public override void Draw()
		{
			var dark = Color.FromArgb(255, 25, 34, 41);
			var metal = Color.FromArgb(255, 63, 78, 84);
			Fill(0, 0, Bounds.Width, Bounds.Height, dark);
			Text("LEFT SIDE VIEW", 16, 8, Color.White);
			if (!Layout.HasChassis) { Text("Choose a chassis on the left to begin.", 104, 180, Color.White); return; }
			var hw = Layout.HullWidth * GridCell(false);
			var hh = Layout.HullHeight * GridCell(false);
			DrawChassis(hw, hh, metal, dark);
			if (Layout.TurretWidth > 0)
			{
				var cell = GridCell(true);
				var tw = Layout.TurretWidth * cell;
				var bottom = TurretOrigin.Y + Layout.TurretHeight * cell + 6;
				if (DroneBody)
				{
					Ellipse(143, 20, tw + 26, bottom - 9, metal);
				}
				else if (LightBody)
				{
					Polygon(metal, new int2(146, bottom), new int2(146, 42), new int2(161, 26),
						new int2(tw + 150, 26), new int2(tw + 166, 44), new int2(tw + 166, bottom));
					Ellipse(182, bottom + 2, 52, 10, metal);
				}
				else
				{
					Polygon(metal, new int2(128, bottom), new int2(132, 44), new int2(158, 18),
						new int2(tw + 146, 18), new int2(tw + 178, 51), new int2(tw + 180, bottom - 10), new int2(tw + 156, bottom + 6));
					Fill(170, 12, 36, 6, metal); Fill(145, 0, 3, 36, metal);
					Ellipse(169, bottom + 2, 86, 16, metal);
				}
				foreach (var weapon in Layout.Placements.Where(p => p.InTurret && p.ModuleId == "weapon" && p.Id != Moving && !DroneBody))
				{
					var size = CustomVehicleSpaceDemo.Size(CustomVehicleSpaceDemo.Definition(weapon.ModuleId), weapon.Rotated);
					var center = TurretOrigin.Y + weapon.Y * cell + size.Height * cell / 2;
					var length = LightBody ? 48 : 90;
					Fill(156 + tw + 8, center - (LightBody ? 3 : 7), length, LightBody ? 6 : 14, metal);
					Fill(156 + tw + length + 2, center - (LightBody ? 5 : 11), 10, LightBody ? 10 : 22, ZoneColor(Zone.Weapon));
				}
				if (!DroneBody) Fill(LightBody ? 204 : 194, bottom + 12, LightBody ? 14 : 34, (Layout.Portrait ? 176 : 188) - bottom - 12, metal);
				Text($"{(DroneBody ? "Mount" : "Turret")} {Layout.TurretWidth} x {Layout.TurretHeight}", 20, 48, Color.White);
				Text($"{Layout.Used(true)} cells used", 20, 65, Color.White);
			}
			foreach (var t in new[] { false, true })
			{
				var o = t ? TurretOrigin : HullOrigin;
				var w = t ? Layout.TurretWidth : Layout.HullWidth;
				var h = t ? Layout.TurretHeight : Layout.HullHeight;
				var cell = GridCell(t);
				for (var y = 0; y < h; y++)
					for (var x = 0; x < w; x++)
					{
						Fill(o.X + x * cell, o.Y + y * cell, cell - 2, cell - 2, ZoneFill(Layout.ZoneAt(t, x, y)));
						Border(o.X + x * cell, o.Y + y * cell, cell - 2, cell - 2, ZoneColor(Layout.ZoneAt(t, x, y)));
					}
			}
			foreach (var p in Layout.Placements)
			{
				if (p.Id == Moving) continue;
				var o = p.InTurret ? TurretOrigin : HullOrigin;
				var cell = GridCell(p.InTurret);
				Item(p.ModuleId, p.Rotated, o.X + p.X * cell, o.Y + p.Y * cell, Tint(p.ModuleId), false, cell);
			}
			Text($"Schematic hull: {Layout.HullWidth} x {Layout.HullHeight} / {Layout.Used(false)} cells used", 72, Bounds.Height - 19, Color.White);
		}
	}

	public sealed class CustomVehicleSpaceCursorWidget : Widget
	{
		public CustomVehicleSpaceWidget Canvas;
		public override bool EventBoundsContains(int2 location) => false;
		public override void Draw() => Canvas?.DrawHeld(RenderBounds);
	}
}
