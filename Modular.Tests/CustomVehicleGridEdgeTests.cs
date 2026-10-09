using NUnit.Framework;
using OpenRA.Mods.CA.Modular;

namespace Modular.Tests;

public class CustomVehicleGridEdgeTests
{
    static CustomVehicleSpaceDemo Empty()
    {
        var layout = new CustomVehicleSpaceDemo();
        layout.SetChassis(false); layout.SetTurret(5); return layout;
    }

    [Test]
    public void EmptyCellsShareExactlyOneEdge()
    {
        var layout = Empty();
        var edges = layout.Edges(false).ToArray();
        Assert.That(edges.Length, Is.EqualTo(8 * 5 + 4 * 9));
        Assert.That(edges.Select(e => (e.X, e.Y, e.Vertical)).Distinct().Count(), Is.EqualTo(edges.Length));
        Assert.That(edges.Count(e => e.Vertical && e.X == 1 && e.Y == 1), Is.EqualTo(1));
        Assert.That(edges.All(e => e.ModuleId == null), Is.True);
    }

    [Test]
    public void TwoByTwoBlockHasNoInnerCrossAndPickupRestoresTheEmptyGrid()
    {
        var layout = Empty();
        Assert.That(layout.Place("engine", false, 1, 1, false), Is.True);
        var id = layout.Placements.Single().Id;
        var edges = layout.Edges(false).ToArray();
        Assert.That(edges.Length, Is.EqualTo(76 - 4));
        Assert.That(edges.Any(e => e.Vertical && e.X == 2 && (e.Y == 1 || e.Y == 2)), Is.False);
        Assert.That(edges.Any(e => !e.Vertical && e.Y == 2 && (e.X == 1 || e.X == 2)), Is.False);
        Assert.That(edges.Count(e => e.ModuleId == "engine"), Is.EqualTo(8), "outer perimeter only");
        Assert.That(layout.Edges(false, id).Count(), Is.EqualTo(76));
        Assert.That(layout.Placements.Single().Id, Is.EqualTo(id), "rendering a pickup must not remove it");
    }

    [Test]
    public void SeparateBlocksOfTheSameKindStillHaveOneSharedBoundary()
    {
        var layout = Empty();
        layout.Place("battery", false, 1, 1, false);
        layout.Place("battery", false, 2, 1, false);
        var edges = layout.Edges(false).ToArray();
        foreach (var y in new[] { 1, 2 })
            Assert.That(edges.Count(e => e.Vertical && e.X == 2 && e.Y == y), Is.EqualTo(1));
        Assert.That(edges.Any(e => !e.Vertical && e.Y == 2 && (e.X == 1 || e.X == 2)), Is.False);
    }

    [Test]
    public void ColorBoundariesDoNotSplitOneRotatedModule()
    {
        var layout = Empty();
        Assert.That(layout.Place("battery", false, 0, 1, true), Is.True);
        Assert.That(layout.ZoneAt(false, 0, 1), Is.Not.EqualTo(layout.ZoneAt(false, 1, 1)));
        Assert.That(layout.Edges(false).Any(e => e.Vertical && e.X == 1 && e.Y == 1), Is.False);
    }

    [Test]
    public void AllStockFootprintsSuppressOnlyTheirOwnInternalEdges()
    {
        foreach (var kind in Enum.GetValues<CustomWeaponKind>())
        {
            var layout = new CustomVehicleSpaceDemo { WeaponKind = kind, AmmunitionId = "medium-tank-shell" };
            layout.LoadStockConfiguration(false);
            foreach (var turret in new[] { false, true })
            {
                var edges = layout.Edges(turret).ToArray();
                Assert.That(edges.Select(e => (e.X, e.Y, e.Vertical)).Distinct().Count(), Is.EqualTo(edges.Length));
                foreach (var edge in edges)
                {
                    var ax = edge.Vertical ? edge.X - 1 : edge.X;
                    var ay = edge.Vertical ? edge.Y : edge.Y - 1;
                    Assert.That(layout.ContainsCell(turret, ax, ay) || layout.ContainsCell(turret, edge.X, edge.Y), Is.True,
                        "no phantom edges in missing cells beside protruding sockets");
                }
                foreach (var p in layout.Placements.Where(p => p.InTurret == turret))
                {
                    var size = CustomVehicleSpaceDemo.Size(layout.ModuleFor(p.ModuleId), p.Rotated);
                    for (var y = 0; y < size.Height; y++)
                    for (var x = 0; x < size.Width; x++)
                    {
                        if (x > 0) Assert.That(edges.Any(e => e.Vertical && e.X == p.X + x && e.Y == p.Y + y), Is.False, kind.ToString());
                        if (y > 0) Assert.That(edges.Any(e => !e.Vertical && e.X == p.X + x && e.Y == p.Y + y), Is.False, kind.ToString());
                    }
                }
            }
        }
    }
}
