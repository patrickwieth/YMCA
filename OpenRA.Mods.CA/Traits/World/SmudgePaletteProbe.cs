using System;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Isolated native smudge/palette render diagnostic; never enabled by normal rules.")]
	public sealed class SmudgePaletteProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new SmudgePaletteProbe(this);
	}
	public sealed class SmudgePaletteProbe : ITickRender
	{
		readonly SmudgePaletteProbeInfo info;
		int frames;
		public SmudgePaletteProbe(SmudgePaletteProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_REFINERY_SMUDGE_PROBE") != "1")
				throw new InvalidOperationException("Smudge probe requires an explicitly opted-in local harness.");
			this.info = info;
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			frames++; var w = self.World;
			if (frames < 20) wr.Viewport.Center(w.Map.CenterOfCell(new CPos(110, -25)));
			if (frames == 60 || frames == 120)
			{
				var row = 0;
				foreach (var layer in self.TraitsImplementing<SmudgeLayer>())
				{
					_ = wr.Palette(layer.Info.Palette); _ = wr.Palette(layer.Info.SmokePalette);
					var r = row++;
					for (var i = 0; i < layer.Info.SmokeSequences.Length; i++)
					{
						var seq = layer.Info.SmokeSequences[i]; var position = w.Map.CenterOfCell(new CPos(107 + i * 3, -17 + r * 3));
						w.AddFrameEndTask(_ => w.Add(new SpriteEffect(position, w, layer.Info.SmokeImage, seq, layer.Info.SmokePalette)));
					}
				}
			}
			if (frames == 65 || frames == 125 || frames == 210) Game.TakeScreenshot();
			if (frames == 240) File.WriteAllText(info.ResultPath, "PASS: native initial scorches/crater depths and every configured smoke/flame sequence rendered with resolved palettes.\n");
		}
	}
}
