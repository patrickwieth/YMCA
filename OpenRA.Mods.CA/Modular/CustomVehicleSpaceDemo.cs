using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Modular
{
	// Deliberately isolated UI experiment: no profile serialization, compiler or combat rules.
	public sealed class CustomVehicleSpaceDemo
	{
		public sealed record Module(string Id, string Label, int Width, int Height, bool Hull, bool Turret);
		public sealed record Placement(int Id, string ModuleId, bool InTurret, int X, int Y, bool Rotated);
		public static readonly IReadOnlyList<Module> Modules = Array.AsReadOnly(new[]
		{
			new Module("weapon", "Waffe", 2, 2, false, true),
			new Module("ammo", "Munition", 1, 2, true, true),
			new Module("pdl", "PDL", 1, 2, false, true),
			new Module("battery", "Akku", 1, 2, true, true),
			new Module("generator", "Generator", 2, 2, true, false),
			new Module("engine", "Motor", 2, 2, true, false),
			new Module("gear", "Fahrwerk", 2, 2, true, false),
			new Module("reflector", "Reflector", 2, 2, true, false)
		});

		readonly List<Placement> placements = new();
		int nextId;
		public IReadOnlyList<Placement> Placements => placements.AsReadOnly();
		public bool Heavy { get; private set; }
		public int HullWidth => Heavy ? 5 : 4;
		public int HullHeight => Heavy ? 6 : 5;
		public int TurretWidth { get; private set; }
		public int TurretHeight => TurretWidth == 0 ? 0 : 4;
		public bool HasPdlWithoutBattery => placements.Any(p => p.ModuleId == "pdl") && !placements.Any(p => p.ModuleId == "battery");

		public void SetChassis(bool heavy)
		{
			Heavy = heavy;
			TurretWidth = 0;
			placements.Clear();
		}

		public void LoadExample()
		{
			SetChassis(Heavy);
			SetTurret(3);
			Place("engine", false, 0, 0, false);
			Place("generator", false, 2, 0, false);
			Place("gear", false, 0, 2, false);
			Place("battery", false, 2, 2, false);
			Place("weapon", true, 0, 0, false);
			Place("ammo", true, 2, 0, false);
			Place("pdl", true, 0, 2, false);
		}

		public void SetTurret(int width)
		{
			if (width != 0 && width != 3 && width != 4) throw new ArgumentOutOfRangeException(nameof(width));
			if (width == TurretWidth) return;
			TurretWidth = width;
			placements.RemoveAll(p => p.InTurret);
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
			if (module == null || (turret ? !module.Turret : !module.Hull)) return false;
			if (movingId != null && !placements.Any(p => p.Id == movingId && p.ModuleId == moduleId)) return false;
			var size = Size(module, rotated);
			if (x < 0 || y < 0 || x > (turret ? TurretWidth : HullWidth) - size.Width || y > (turret ? TurretHeight : HullHeight) - size.Height) return false;
			for (var dy = 0; dy < size.Height; dy++)
				for (var dx = 0; dx < size.Width; dx++)
				{
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
