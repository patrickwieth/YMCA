using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Opt-in local foundation connection/render lifecycle diagnostic. Never enabled by normal rules.")]
	public class BibConnectionTestProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly string Building = "powr.soviet";
		public readonly string Defense = "ttur";
		public override object Create(ActorInitializer init) => new BibConnectionTestProbe(this);
	}

	public class BibConnectionTestProbe : ITickRender
	{
		readonly BibConnectionTestProbeInfo info;
		int frames;
		public BibConnectionTestProbe(BibConnectionTestProbeInfo info) { this.info = info; }

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			frames++;
			var world = self.World;
			if (frames < 20)
				wr.Viewport.Center(world.Map.CenterOfCell(new CPos(111, -24)));
			if (frames == 180)
			{
				Check(world, 8, "initial cardinal/diagonal connections and owner isolation");
				Game.TakeScreenshot();
			}
			if (frames == 210)
				world.AddFrameEndTask(_ => At(world, 108, -30).Dispose());
			if (frames == 250)
				Check(world, 5, "source removal clears its three links");
			if (frames == 280)
				world.AddFrameEndTask(_ => At(world, 112, -22).ChangeOwner(world.Players.Single(p => p.InternalName == "BibRed")));
			if (frames == 320)
				Check(world, 2, "capture by a different owner clears links");
			if (frames == 350)
				world.AddFrameEndTask(_ => At(world, 108, -22).ChangeOwner(world.Players.Single(p => p.InternalName == "BibRed")));
			if (frames == 390)
			{
				Check(world, 5, "matching new owners restore links and remap");
				Game.TakeScreenshot();
			}
			if (frames == 430)
				world.AddFrameEndTask(w => w.CreateActor(info.Building, new TypeDictionary
				{
					new OwnerInit(w.Players.Single(p => p.InternalName == "BibRed")),
					new LocationInit(new CPos(108, -30)), new SkipMakeAnimsInit()
				}));
			if (frames == 470)
				Check(world, 8, "new building creates links");
			if (frames == 510)
				world.AddFrameEndTask(_ => At(world, 108, -30).Kill(At(world, 112, -30)));
			if (frames == 550)
			{
				Check(world, 5, "destroyed building clears links");
			}
			if (frames == 580)
				world.AddFrameEndTask(w => w.Actors.Single(a => a.Info.Name == info.Defense && a.Location == new CPos(105, -26)).Dispose());
			if (frames == 620)
			{
				Check(world, 4, "diagonal source removal clears its link");
				File.AppendAllText(info.ResultPath, "PASS: fixed-grid native rendering, diagonal/cardinal links, removal, capture, placement and destruction.\n");
			}
		}

		Actor At(World world, int x, int y) => world.Actors.Single(a => a.Info.Name == info.Building && a.Location == new CPos(x, y));

		void Check(World world, int expected, string stage)
		{
			var count = world.WorldActor.Trait<BibConnectionLayer>().ConnectionCount;
			File.AppendAllText(info.ResultPath, $"{stage}: {count} (expected {expected})\n");
			if (count != expected)
				throw new InvalidOperationException("Bib lifecycle diagnostic failed: " + stage);
		}
	}
}
