using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Modular
{
	// Isolated spatial UI experiment: no profile serialization or combat rules.
	public sealed class CustomVehicleSpaceDemo
	{
		public enum Zone { None, Interior, RunningGear, Weapon, Armor, Ammunition, Any }
		public sealed record Module(string Id, string Label, int Width, int Height, bool Hull, bool Turret, Zone Mount = Zone.Interior);
		public sealed record Placement(int Id, string ModuleId, bool InTurret, int X, int Y, bool Rotated);
		public static readonly IReadOnlyList<Module> Modules = Array.AsReadOnly(new[]
		{
			new Module("weapon", "Weapon", 2, 2, false, true, Zone.Weapon),
			new Module("ammo", "Ammunition", 1, 2, false, true, Zone.Ammunition),
			new Module("pdl", "PDL", 1, 2, true, true, Zone.Any),
			new Module("battery", "Battery", 1, 2, true, true, Zone.Any),
			new Module("generator", "Generator", 2, 2, true, false),
			new Module("engine", "Engine", 2, 2, true, false),
			new Module("gear", "Running gear", 4, 1, true, false, Zone.RunningGear),
			new Module("armor", "Armor plate", 2, 1, true, false, Zone.Armor),
			new Module("reflector", "Reflector armor", 2, 1, true, false, Zone.Armor)
		});

		readonly List<Placement> placements = new();
		int nextId;
		public IReadOnlyList<Placement> Placements => placements.AsReadOnly();
		public bool HasChassis { get; private set; }
		public bool Heavy { get; private set; }
		public int HullWidth => !HasChassis ? 0 : Heavy ? 10 : 8;
		public int HullHeight => !HasChassis ? 0 : Heavy ? 5 : 4;
		public int TurretWidth { get; private set; }
		public int TurretHeight => TurretWidth == 0 ? 0 : 3;
		public bool HasPdlWithoutBattery => placements.Any(p => p.ModuleId == "pdl") && !placements.Any(p => p.ModuleId == "battery");

		public void SetChassis(bool heavy)
		{
			HasChassis = true;
			Heavy = heavy;
			TurretWidth = 0;
			placements.Clear();
		}

		public void LoadStockConfiguration(bool heavy)
		{
			SetChassis(heavy); SetTurret(5);
			Place("engine", false, 1, 1, false); Place("generator", false, 3, 1, false);
			Place("gear", false, 1, HullHeight - 1, false); Place("armor", false, 0, 0, false);
			Place("weapon", true, 3, 1, false); Place("ammo", true, 0, 0, false);
		}

		public void LoadExample()
		{
			SetChassis(Heavy);
			SetTurret(5);
			Place("engine", false, 1, 1, false);
			Place("generator", false, 3, 1, false);
			Place("gear", false, 1, HullHeight - 1, false);
			Place("battery", false, 5, 1, false);
			Place("armor", false, 0, 0, false);
			Place("weapon", true, 3, 1, false);
			Place("ammo", true, 0, 0, false);
			Place("pdl", true, 1, 0, false);
		}

		public void SetTurret(int width)
		{
			if (width != 0 && width != 5) throw new ArgumentOutOfRangeException(nameof(width));
			if (width != 0 && !HasChassis) throw new InvalidOperationException("Choose a chassis first.");
			if (width == TurretWidth) return;
			TurretWidth = width;
			placements.RemoveAll(p => p.InTurret);
		}

		// Front is right. Bottom takes precedence over the blue hull perimeter.
		public Zone ZoneAt(bool turret, int x, int y)
		{
			var w = turret ? TurretWidth : HullWidth;
			var h = turret ? TurretHeight : HullHeight;
			if (x < 0 || y < 0 || x >= w || y >= h) return Zone.None;
			if (turret) return x == w - 1 ? Zone.Weapon : Zone.Ammunition;
			if (y == h - 1) return Zone.RunningGear;
			if (x == 0 || x == w - 1 || y == 0) return Zone.Armor;
			return Zone.Interior;
		}

		public static Module Definition(string id) => Modules.FirstOrDefault(m => m.Id == id);
		public static (int Width, int Height) Size(Module module, bool rotated) => rotated ? (module.Height, module.Width) : (module.Width, module.Height);
		public Placement At(bool turret, int x, int y) => placements.FirstOrDefault(p =>
		{
			var size = Size(Definition(p.ModuleId), p.Rotated);
			return p.InTurret == turret && x >= p.X && y >= p.Y && x < p.X + size.Width && y < p.Y + size.Height;
		});

		public bool CanPlace(string moduleId, bool turret, int x, int y, bool rotated, int? movingId = null)
		{
			var module = Definition(moduleId);
			if (!HasChassis || module == null || (turret ? !module.Turret : !module.Hull)) return false;
			if (movingId != null && !placements.Any(p => p.Id == movingId && p.ModuleId == moduleId)) return false;
			var size = Size(module, rotated);
			if (x < 0 || y < 0 || x > (turret ? TurretWidth : HullWidth) - size.Width || y > (turret ? TurretHeight : HullHeight) - size.Height) return false;
			if (module.Mount == Zone.Weapon && x + size.Width != TurretWidth) return false;
			for (var dy = 0; dy < size.Height; dy++)
				for (var dx = 0; dx < size.Width; dx++)
				{
					var zone = ZoneAt(turret, x + dx, y + dy);
					if (module.Mount != Zone.Any && (module.Mount == Zone.Weapon
						? zone != Zone.Weapon && zone != Zone.Ammunition : zone != module.Mount)) return false;
					var occupant = At(turret, x + dx, y + dy);
					if (occupant != null && occupant.Id != movingId) return false;
				}
			return true;
		}

		public bool Place(string moduleId, bool turret, int x, int y, bool rotated, int? movingId = null)
		{
			if (!CanPlace(moduleId, turret, x, y, rotated, movingId)) return false;
			if (movingId != null) placements.RemoveAll(p => p.Id == movingId);
			placements.Add(new Placement(movingId ?? ++nextId, moduleId, turret, x, y, rotated));
			return true;
		}

		public void Remove(int id) => placements.RemoveAll(p => p.Id == id);
		public int Used(bool turret) => placements.Where(p => p.InTurret == turret).Sum(p => { var m = Definition(p.ModuleId); return m.Width * m.Height; });
	}
}
