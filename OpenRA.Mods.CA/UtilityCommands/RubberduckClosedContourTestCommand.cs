using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckClosedContourTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-closed-contour-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || args.Length == 2;
		[Desc("[MAP_DIRECTORY]", "Check closed contours, composition parity and optionally audit exported closed actors recursively.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var accepted = 0; var rejected = 0;
			void Reject(Action action)
			{
				try { action(); }
				catch (InvalidDataException) { rejected++; return; }
				throw new InvalidDataException("Broken closed contour was accepted.");
			}
			foreach (var width in new[] { 3, 5, 9, 17 })
				foreach (var height in new[] { 3, 6, 11 })
					foreach (var translation in new[] { new CVec(0, 0), new CVec(107, -43), new CVec(-37, 81) })
						for (var shape = 0; shape < 5; shape++)
						{
							var roof = (from x in Enumerable.Range(0, width)
								from y in Enumerable.Range(0, height)
								where !(shape == 1 && x < width / 2 && y < height / 2 ||
									shape == 2 && x >= width - width / 2 && y < height / 2 ||
									shape == 3 && x >= width - width / 2 && y >= height - height / 2 ||
									shape == 4 && x < width / 2 && y >= height - height / 2)
								select new CVec(x, y) + translation).ToArray();
							var contour = RubberduckClosedCliffContour.Create(roof); accepted++;
							if (!contour.SequenceEqual(RubberduckClosedCliffContour.Create(roof.Reverse())))
								throw new InvalidDataException("Contour depends on input enumeration order.");
							Reject(() => RubberduckClosedCliffContour.Validate(contour.Skip(1).ToArray(), roof));
							Reject(() => RubberduckClosedCliffContour.Validate(contour.Concat(new[] { contour[0] }).ToArray(), roof));
							var moved = contour.ToList(); moved[0] = (moved[0].Slot, moved[0].Offset + new CVec(1, 0));
							Reject(() => RubberduckClosedCliffContour.Validate(moved, roof));
						}
			Reject(() => RubberduckClosedCliffContour.Create(Array.Empty<CVec>()));
			Reject(() => RubberduckClosedCliffContour.Create(new[] { new CVec(0, 0), new CVec(1, 1) }));
			Reject(() => RubberduckClosedCliffContour.Create(from x in Enumerable.Range(0, 3)
				from y in Enumerable.Range(0, 3) where x != 1 || y != 1 select new CVec(x, y)));
			for (var notch = -1; notch < 4; notch++)
				if (RubberduckOriginalClosedReference.Walls(notch).Count != 24) throw new InvalidDataException("Calibration closure changed.");

			var reservation = new MapPlan(48, 48);
			for (var x = 0; x < reservation.Width; x++)
				for (var y = 0; y < reservation.Height; y++) reservation.SetTerrain(x, y, PlannedTerrain.Land);
			reservation.Spawns.Add(new PlanPoint(2, 2));
			foreach (var pair in PlateauTopology.CalibrationPlateau(MountainPlateauPlanner.Cell(24, 24)))
			{
				var point = MountainPlateauPlanner.Point(pair.Key);
				reservation.PlateauSurfaces.Add(point, pair.Value);
				if (pair.Value.Blocked) reservation.SetTerrain(point.U, point.V, PlannedTerrain.Mountain);
			}
			RubberduckNativeCliffPlanner.Apply(reservation);
			var protectedFeet = reservation.PlateauSurfaces.Where(p => p.Value.Height == 0 && p.Value.Blocked).ToDictionary(p => p.Key, p => p.Value);
			if (protectedFeet.Count == 0) throw new InvalidDataException("Foot regression has no reserved feet.");
			foreach (var foot in protectedFeet.Keys) reservation.AddFeature(foot.U, foot.V, PlannedFeature.BuildClearance);
			reservation.NativeCliffFaces.Clear(); reservation.NativeCliffPieces.Clear();
			RubberduckNativeCliffPlanner.Apply(reservation);
			if (reservation.NativeCliffPieces.Count == 0) throw new InvalidDataException("Preserving protected feet discarded all native faces.");
			foreach (var foot in protectedFeet.Keys)
				if (!reservation.PlateauSurfaces.TryGetValue(foot, out var value) || value.Height != 0 || value.Ramp != 0 || !value.Blocked ||
					reservation.TerrainAt(foot.U, foot.V) != PlannedTerrain.Mountain)
					throw new InvalidDataException("Native replan modified an existing protected foot.");

			Game.ModData = utility.ModData;
			var terrain = (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"];
			foreach (var apply in new[] { false, true })
			{
				using var expected = new Map(utility.ModData, terrain, 82, 98);
				using var actual = new Map(utility.ModData, terrain, 82, 98);
				expected.SetBounds(new PPos(1, 17), new PPos(80, 96));
				actual.SetBounds(new PPos(1, 17), new PPos(80, 96));
				foreach (var c in actual.AllCells) { actual.Tiles[c] = new TerrainTile(1000, 0); expected.Tiles[c] = new TerrainTile(1000, 0); }
				var center = new MPos(40, 56).ToCPos(actual);
				var surfaces = PlateauTopology.CalibrationPlateau(center);
				var faces = new HashSet<(CPos, int)> { (center + new CVec(7, 4), 0), (center + new CVec(4, 7), 1) };
				var pieces = new[] { (Cell: center + new CVec(7, 4), Slot: 5), (Cell: center + new CVec(4, 7), Slot: 0) };
				var prefix = apply ? "NativeCliff" : "Native";
				var before = new List<MiniYamlNode> { new MiniYamlNode("Existing", new ActorReference("mpspawn") { new LocationInit(center) }.Save()) };
				var after = before.ToList();
				// Preserve the former implementation as an independent order/ID oracle.
				if (apply) RubberduckPlateauRenderer.Apply(expected, surfaces, before, (c, d) => !faces.Contains((c, d)));
				else RubberduckPlateauRenderer.AddActors(expected, surfaces, before, (c, d) => !faces.Contains((c, d)));
				foreach (var piece in pieces)
					before.Add(new MiniYamlNode(prefix + before.Count, new ActorReference("terrain.rubberduck.nativecliff" + piece.Slot)
					{ new LocationInit(piece.Cell), new OwnerInit("Neutral") }.Save()));
				RubberduckPlateauRenderer.ComposeNative(actual, surfaces, after, faces, pieces, prefix, apply);
				var complete = PlateauTopology.WithCliffFeet(surfaces);
				var selected = new RubberduckClosedCliffSelection(actual, complete);
				if (selected.Cells.Count != surfaces.Count || selected.Pieces.Count != 52)
					throw new InvalidDataException($"Complete four-ramp component selected {selected.Cells.Count} cells and {selected.Pieces.Count} pieces instead of {surfaces.Count}/52.");
				if (selected.Pieces.Any(p => !PlateauRemovalPlan.Decoration("terrain.rubberduck.closedcliff" + p.Slot)))
					throw new InvalidDataException("Closed family is not recognized by normal removal tools.");
				complete[selected.Pieces[0].Cell] = new PlateauSurface(0);
				if (new RubberduckClosedCliffSelection(actual, complete).Pieces.Count != 0)
					throw new InvalidDataException("Closed family encroached on an unreserved foot.");
				if (before.WriteToString() != after.WriteToString()) throw new InvalidDataException("Shared composition changed actor identities or ordering.");
				foreach (var c in actual.AllCells)
					if (!actual.Tiles[c].Equals(expected.Tiles[c]) || actual.Height[c] != expected.Height[c] || actual.Ramp[c] != expected.Ramp[c] ||
						!actual.Resources[c].Equals(expected.Resources[c])) throw new InvalidDataException("Shared composition changed terrain.");
				complete[selected.Pieces[0].Cell] = new PlateauSurface(0, 0, true);
				var closedActors = new List<MiniYamlNode>();
				RubberduckPlateauRenderer.ComposeNative(actual, complete, closedActors, new HashSet<(CPos, int)>(), Array.Empty<(CPos, int)>(), "Closed", true);
				var removal = PlateauRemovalPlan.Create(actual, closedActors, center);
				if (removal.Patch.Count != complete.Count || removal.Decorations.Count != closedActors.Count)
					throw new InvalidDataException("Normal lowering lost closed-family feet or actors.");
				var growth = PlateauShapePlan.Create(actual, closedActors, new[] { center + new CVec(-7, 8) }, true, Array.Empty<CPos>());
				if (growth.Patch.Count != 4 || growth.AfterActors.Count(n => n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)) != 54)
					throw new InvalidDataException("Normal growth discarded existing closed-family reservations.");
			}
			using (var oldMountain = new Map(utility.ModData, terrain, 82, 98))
			using (var newMountain = new Map(utility.ModData, terrain, 82, 98))
			{
				oldMountain.SetBounds(new PPos(1, 17), new PPos(80, 96));
				newMountain.SetBounds(new PPos(1, 17), new PPos(80, 96));
				foreach (var c in oldMountain.AllCells) { oldMountain.Tiles[c] = new TerrainTile(1000, 0); newMountain.Tiles[c] = new TerrainTile(1000, 0); }
				var center = new MPos(40, 56).ToCPos(oldMountain);
				var mountain = RubberduckOriginalClosedReference.Roof(0).Select(c => center + c).ToDictionary(c => c, _ => new PlateauSurface(4, 0, true));
				var before = new List<MiniYamlNode>(); var after = new List<MiniYamlNode>();
				RubberduckPlateauRenderer.Apply(oldMountain, mountain, before);
				RubberduckMountainRenderer.Apply(newMountain, mountain.Keys, after);
				if (before.Count != after.Count) throw new InvalidDataException("Mountain material changed actor coverage.");
				for (var i = 0; i < before.Count; i++)
				{
					var type = before[i].Value.Value;
					if (type.StartsWith("terrain.rubberduck.plateauwall", StringComparison.Ordinal))
						type = "terrain.rubberduck.mountainwall" + int.Parse(type.Substring("terrain.rubberduck.plateauwall".Length)) / 25;
					if (after[i].Key != before[i].Key || after[i].Value.Value != type ||
						before[i].Value.Nodes.WriteToString() != after[i].Value.Nodes.WriteToString() || !PlateauRemovalPlan.Decoration(type))
						throw new InvalidDataException("Mountain material changed actor anchors, IDs or editor support.");
				}
				foreach (var c in oldMountain.AllCells)
					if (oldMountain.Height[c] != newMountain.Height[c] || !oldMountain.Tiles[c].Equals(newMountain.Tiles[c]) || !oldMountain.Resources[c].Equals(newMountain.Resources[c]))
						throw new InvalidDataException("Mountain material changed terrain, collision or resources.");
			}
			for (var turn = 0; turn < 4; turn++)
			{
				CPos Rotate(CVec c)
				{
					var x = c.X; var y = c.Y;
					for (var i = 0; i < turn; i++) (x, y) = (-y, x);
					return new CPos(x, y);
				}
				var roof = RubberduckOriginalClosedReference.Roof(0).Select(Rotate).ToHashSet();
				var cornerSurfaces = roof.ToDictionary(c => c, _ => new PlateauSurface(4));
				cornerSurfaces[Rotate(new CVec(2, -2))] = new PlateauSurface(3, (byte)(turn + 1));
				cornerSurfaces = PlateauTopology.WithCliffFeet(cornerSurfaces);
				bool Foot(CPos c, bool blocked) => cornerSurfaces.TryGetValue(c, out var s) && s.Height == 0 && s.Ramp == 0 && s.Blocked == blocked;
				if (!RubberduckClosedCliffSelection.TryPieces(cornerSurfaces, roof, Foot, out _))
					throw new InvalidDataException("A diagonal interior ramp incorrectly rejected a flat corner socket.");
				cornerSurfaces[Rotate(new CVec(2, -2))] = new PlateauSurface(2, (byte)(turn + 1));
				if (RubberduckClosedCliffSelection.TryPieces(cornerSurfaces, roof, Foot, out _))
					throw new InvalidDataException("An interior ramp missed the corner's height-four vertex.");
				cornerSurfaces[Rotate(new CVec(2, -2))] = new PlateauSurface(3, (byte)(turn + 1));
				cornerSurfaces[Rotate(new CVec(2, -1))] = new PlateauSurface(3, (byte)(turn + 1));
				if (RubberduckClosedCliffSelection.TryPieces(cornerSurfaces, roof, Foot, out _))
					throw new InvalidDataException("A ramp-owned corner was treated as a flat socket.");
			}
			var planning = new MapPlan(80, 96);
			var planningCenter = MountainPlateauPlanner.Cell(40, 48);
			var outline = new HashSet<CPos>();
			for (var x = -13; x <= 13; x++)
				for (var y = -13; y <= 13; y++)
					if (x * x + y * y <= 160 && !(x > 3 && y > 4)) outline.Add(planningCenter + new CVec(x, y));
			var coarse = MountainPlateauPlanner.CoarseRoof(outline, planningCenter);
			for (var transform = 0; transform < 8; transform++)
			{
				CPos Transform(CPos c)
				{
					var delta = c - planningCenter; var x = transform >= 4 ? -delta.X : delta.X; var y = delta.Y;
					for (var turn = 0; turn < transform % 4; turn++) (x, y) = (-y, x);
					return planningCenter + new CVec(x, y);
				}
				if (!coarse.Select(Transform).ToHashSet().SetEquals(MountainPlateauPlanner.CoarseRoof(outline.Select(Transform).ToHashSet(), planningCenter)))
					throw new InvalidDataException("Roof fitting introduced a compass bias.");
			}
			var rectangle = Enumerable.Range(-5, 6).SelectMany(x => Enumerable.Range(-2, 5).Select(y => planningCenter + new CVec(x, y))).ToHashSet();
			rectangle.Add(planningCenter + new CVec(-2, 3));
			var fitted = MountainPlateauPlanner.RampRectangle(rectangle, planningCenter, new CVec(1, 0), new CVec(0, 1));
			if (fitted == null || fitted.Count != 30 || !fitted.IsSubsetOf(rectangle))
				throw new InvalidDataException("Small-island rectangle fitting lost its six-row ramp footprint.");
			var clone = planning.Clone();
			clone.SetTerrain(1, 1, PlannedTerrain.Mountain); clone.SetHomeOwner(1, 1, 0);
			clone.PlateauSurfaces.Add(new MPos(1, 1), new PlateauSurface(4));
			clone.Spawns.Add(new PlanPoint(1, 1)); clone.ValidationMessages.Add("draft");
			if (planning.TerrainAt(1, 1) != PlannedTerrain.Land || planning.HomeOwnerAt(1, 1) != -1 || planning.FeaturesAt(1, 1) != PlannedFeature.None ||
				planning.PlateauSurfaces.Count != 0 || planning.Spawns.Count != 0 || planning.ValidationMessages.Count != 0)
				throw new InvalidDataException("An uncommitted symmetry orbit modified its original plan.");
			var source = PlateauTopology.CalibrationPlateau(planningCenter);
			source[planningCenter + new CVec(7, 2)] = new PlateauSurface(1, 0, true);
			var reserved = MountainPlateauPlanner.ReserveClosedFeet(planning, source);
			if (ReferenceEquals(source, reserved) || reserved.Count != source.Count + 52 ||
				reserved[planningCenter + new CVec(7, 2)].Height != 4 || source[planningCenter + new CVec(7, 2)].Height != 1)
				throw new InvalidDataException("Planning did not reserve a complete ring without mutating its input.");
			foreach (var pair in source.Where(p => !p.Value.Blocked))
				if (!reserved[pair.Key].Equals(pair.Value)) throw new InvalidDataException("Foot planning changed a walkable roof or ramp.");
			var occupied = MountainPlateauPlanner.Point(reserved.Keys.First(c => !source.ContainsKey(c)));
			foreach (var feature in new[] { PlannedFeature.Resource, PlannedFeature.Spawn, PlannedFeature.Entrance, PlannedFeature.BuildClearance, PlannedFeature.TechBuilding })
			{
				planning.AddFeature(occupied.U, occupied.V, feature);
				var messages = planning.ValidationMessages.Count;
				if (!ReferenceEquals(source, MountainPlateauPlanner.ReserveClosedFeet(planning, source)) || planning.ValidationMessages.Count != messages)
					throw new InvalidDataException("Rejected foot planning mutated the plan or ignored protection.");
				planning.RemoveFeature(occupied.U, occupied.V, feature);
			}
			for (var x = 0; x < planning.Width; x++)
				for (var y = 0; y < planning.Height; y++)
					if (planning.TerrainAt(x, y) != PlannedTerrain.Land || planning.FeaturesAt(x, y) != PlannedFeature.None)
						throw new InvalidDataException("Foot planning committed terrain before validation.");
			if (args.Length == 2)
			{
				var paths = Directory.GetFiles(args[1], "*.oramap", SearchOption.AllDirectories);
				if (paths.Length == 0) throw new InvalidDataException("No exported maps to audit.");
				foreach (var path in paths)
				{
					using var input = File.OpenRead(path);
					using var package = new ZipFileLoader.ReadOnlyZipFile(input, path);
					using var map = new Map(utility.ModData, package);
					var surfaces = map.AllCells.Where(c => map.Contains(c) && (map.Height[c] != 0 || map.Ramp[c] != 0 || map.Tiles[c].Type == 3992))
						.ToDictionary(c => c, c => new PlateauSurface(map.Height[c], map.Ramp[c], map.Tiles[c].Type == 3992));
					var selected = new RubberduckClosedCliffSelection(map, surfaces).Pieces.ToHashSet();
					var actors = map.ActorDefinitions.Where(n => n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)).ToArray();
					if (actors.Length == 0 || map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.nativecliff", StringComparison.Ordinal)))
						throw new InvalidDataException("Export retains a legacy plateau or has no closed actors: " + path);
					var legacyFeet = LegacyCliffAbutments.Select(map, map.ActorDefinitions.Select(n =>
					{
						var actor = new ActorReference(n.Value.Value, n.Value.ToDictionary());
						return (map.Rules.Actors[actor.Type], actor.Get<LocationInit>().Value);
					}));
					if (legacyFeet.Count != 0) throw new InvalidDataException("Closed export acquired legacy foot decorations: " + path);
					foreach (var node in actors)
					{
						var actor = new ActorReference(node.Value.Value, node.Value.ToDictionary());
						var cell = actor.Get<LocationInit>().Value;
						var slot = int.Parse(node.Value.Value.Substring("terrain.rubberduck.closedcliff".Length));
						if (!selected.Remove((cell, slot))) throw new InvalidDataException("Exported actor lost its unique reserved socket: " + path);
					}
					Console.WriteLine($"AUDIT PASS: {Path.GetFileName(path)} ({actors.Length} closed actors).");
				}
				Console.WriteLine($"PASS: {paths.Length} exported maps retain unique closed-actor sockets under current validation.");
			}
			Console.WriteLine($"PASS: {accepted} deterministic general contours, {rejected} rejected corrupt contours, five original fixtures, {protectedFeet.Count} protected feet preserved, export/editor composition parity.");
		}
	}
}
