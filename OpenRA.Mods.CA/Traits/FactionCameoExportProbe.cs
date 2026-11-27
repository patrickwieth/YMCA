using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class FactionCameoExportProbeInfo : TraitInfo
	{
		public readonly string CatalogPath = "";
		public override object Create(ActorInitializer init) => new FactionCameoExportProbe(this);
	}

	public class FactionCameoExportProbe : ITick, IPostWorldLoaded
	{
		readonly FactionCameoExportProbeInfo info;
		WorldRenderer wr;
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer renderer) { wr = renderer; }
		JArray catalog;
		int page;
		int ticks;
		bool captured;
		bool done;
		readonly List<Widget> widgets = new();
		readonly List<object> pages = new();
		public FactionCameoExportProbe(FactionCameoExportProbeInfo info) { this.info = info; }

		static bool Complete(string path)
		{
			try
			{
				using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				if (s.Length < 12) return false;
				s.Seek(-12, SeekOrigin.End);
				var end = new byte[12];
				return s.Read(end,0,12)==12 && end.SequenceEqual(new byte[] {0,0,0,0,73,69,78,68,174,66,96,130});
			}
			catch (IOException) { return false; }
		}

		void ITick.Tick(Actor self)
		{
			if (done || Environment.GetEnvironmentVariable("YMCA_CAMEO_EXPORT") != "1") return;
			ticks++;
			if (ticks < 35) return;
			if (catalog == null) catalog = JArray.Parse(File.ReadAllText(info.CatalogPath));
			if (widgets.Count == 0)
			{
				if (Math.Abs(Game.Renderer.WindowScale - 1) > .001) throw new InvalidDataException("Source export requires 1x UI.");
				for (var i = 0; i < 6 && page * 6 + i < catalog.Count; i++)
				{
					var row = catalog[page*6+i];
					var actor = self.World.Map.Rules.Actors[(string)row["actor"]];
					var player = self.World.Players.Single(p => p.InternalName == (string)row["owner"]);
					var angle = new WAngle((int)row["facing"]);
					var init = new TypeDictionary { new OwnerInit(player), new FactionInit((string)row["faction"]), new FacingInit(WAngle.Zero), new TurretFacingInit(angle) };
					var widget = new BuildingCameoSourceWidget(wr, actor, init, new Rectangle(8+i%3*776,8+i/3*584,768,576), (float)row["crop"], (float)row["focusX"], (float)row["focusY"]);
					Ui.Root.AddChild(widget); widgets.Add(widget);
				}
				ticks=0;captured=false;
				return;
			}
			if (!captured)
			{
				if (ticks < 40) return;
				Game.TakeScreenshot();captured=true;
				return;
			}
			var folder=Path.Combine(Platform.SupportDir,"Screenshots");
			if (!Directory.Exists(folder)) return;
			var shots=Directory.GetFiles(folder,"*.png",SearchOption.AllDirectories).OrderBy(p=>p).ToArray();
			if (shots.Length <= page || !Complete(shots[page])) return;
			pages.Add(new { Page=page, Screenshot=Path.GetRelativePath(Platform.SupportDir,shots[page]), Actors=catalog.Skip(page*6).Take(6).Select(r=>(string)r["key"]).ToArray() });
			foreach (var widget in widgets) Ui.Root.RemoveChild(widget);
			widgets.Clear();page++;
			if (page*6 >= catalog.Count)
			{
				done=true;
				File.WriteAllText(Path.Combine(Platform.SupportDir,"cameo-export.json"),JsonConvert.SerializeObject(new { Passed=true, Pages=pages },Formatting.Indented));
			}
		}
	}
}
