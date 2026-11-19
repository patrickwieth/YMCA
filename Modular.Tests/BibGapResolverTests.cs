using NUnit.Framework;
using OpenRA.Mods.CA.Compatibility;
using static OpenRA.Mods.CA.Compatibility.BibGapResolver;

namespace Modular.Tests
{
	[TestFixture]
	public class BibGapResolverTests
	{
		static Gap[] Gaps(params Cell[] cells) => Resolve(cells, new HashSet<(int, int)>()).ToArray();

		[TestCase(2, 0, 1, 0)]
		[TestCase(0, 2, 0, 1)]
		[TestCase(-2, 0, -1, 0)]
		[TestCase(0, -2, 0, -1)]
		[TestCase(2, 2, 1, 1)]
		[TestCase(2, -2, 1, -1)]
		[TestCase(-2, 2, -1, 1)]
		[TestCase(-2, -2, -1, -1)]
		public void FillsExactlyOneCell(int x, int y, int gx, int gy)
		{
			var gap = Gaps(new Cell(0, 0, 1, "A"), new Cell(x, y, 2, "A")).Single();
			Assert.That((gap.X, gap.Y), Is.EqualTo((gx, gy)));
		}

		[TestCase(1, 0)]
		[TestCase(3, 0)]
		[TestCase(1, 1)]
		[TestCase(3, 3)]
		[TestCase(2, 1)]
		public void IgnoresAdjacentWideAndOffAxisGaps(int x, int y) =>
			Assert.That(Gaps(new Cell(0, 0, 1, "A"), new Cell(x, y, 2, "A")), Is.Empty);

		[Test]
		public void RequiresSameOwnerNotAlliance() =>
			Assert.That(Gaps(new Cell(0, 0, 1, "A"), new Cell(2, 0, 2, "B")), Is.Empty);

		[Test]
		public void RequiresDistinctBuildings() =>
			Assert.That(Gaps(new Cell(0, 0, 1, "A"), new Cell(2, 0, 1, "A")), Is.Empty);

		[Test]
		public void BlocksOccupiedCells()
		{
			var cells = new[] { new Cell(0, 0, 1, "A"), new Cell(2, 0, 2, "A") };
			Assert.That(Resolve(cells, new HashSet<(int, int)> { (1, 0) }), Is.Empty);
			Assert.That(Gaps(cells.Append(new Cell(1, 0, 3, "B")).ToArray()), Is.Empty);
		}

		[Test]
		public void RemovalAndCaptureRemoveLinks()
		{
			var left = new Cell(0, 0, 1, "A");
			Assert.That(Gaps(left, new Cell(2, 0, 2, "A")), Has.Length.EqualTo(1));
			Assert.That(Gaps(left), Is.Empty);
			Assert.That(Gaps(left, new Cell(2, 0, 2, "B")), Is.Empty);
			Assert.That(Gaps(new Cell(0, 0, 1, "B"), new Cell(2, 0, 2, "B")), Has.Length.EqualTo(1));
		}

		[Test]
		public void DoesNotPropagateFromGeneratedCells()
		{
			var result = Gaps(new Cell(0, 0, 1, "A"), new Cell(2, 0, 2, "A"), new Cell(1, 2, 3, "A"));
			Assert.That(result.Select(g => (g.X, g.Y)), Is.EqualTo(new[] { (1, 0) }));
		}

		[Test]
		public void CrossingLinksAreDeduplicated()
		{
			var cells = new[] { new Cell(0, 1, 1, "A"), new Cell(2, 1, 2, "A"), new Cell(1, 0, 3, "A"), new Cell(1, 2, 4, "A") };
			Assert.That(Gaps(cells), Has.Length.EqualTo(1));
			Assert.That(Gaps(cells.Reverse().ToArray()).Select(g => (g.X, g.Y, g.First, g.Second)),
				Is.EqualTo(Gaps(cells).Select(g => (g.X, g.Y, g.First, g.Second))));
		}

		[Test]
		public void CrossingOwnersDoNotClaimTheSameGap() => Assert.That(
			Gaps(new Cell(0, 1, 1, "A"), new Cell(2, 1, 2, "A"), new Cell(1, 0, 3, "B"), new Cell(1, 2, 4, "B")), Is.Empty);

		[Test]
		public void DiagonalOwnershipOccupancyAndRemoval()
		{
			var first = new Cell(0, 0, 1, "A");
			var second = new Cell(2, 2, 2, "A");
			Assert.That(Gaps(first, second), Has.Length.EqualTo(1));
			Assert.That(Gaps(first), Is.Empty);
			Assert.That(Gaps(first, new Cell(2, 2, 2, "B")), Is.Empty);
			Assert.That(Resolve(new[] { first, second }, new HashSet<(int, int)> { (1, 1) }), Is.Empty);
			Assert.That(Gaps(first, second, new Cell(1, 1, 3, "A")), Is.Empty);
		}

		[Test]
		public void ContestedAnchorDoesNotConnect() => Assert.That(
			Gaps(new Cell(0, 0, 1, "A"), new Cell(0, 0, 3, "B"), new Cell(2, 0, 2, "A")), Is.Empty);
	}
}
