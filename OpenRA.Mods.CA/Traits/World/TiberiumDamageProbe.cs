using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class TiberiumDamageProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly string Mode = "default";
		public override object Create(ActorInitializer init) => new TiberiumDamageProbe(init,this);
	}

	public sealed class TiberiumDamageProbe : ITick
	{
		readonly TiberiumDamageProbeInfo info;
		readonly Dictionary<Actor,int> initial = new();
		int ticks;
		public TiberiumDamageProbe(ActorInitializer init,TiberiumDamageProbeInfo info)
		{
			if(Environment.GetEnvironmentVariable("YMCA_TIBERIUM_DAMAGE_PROBE")!="1") throw new InvalidOperationException("Diagnostic opt-in required.");
			this.info=info;
			// World traits are constructed before Player actors. In this isolated
			// single-player fixture, exercise the actual lobby option consumed by
			// LobbyPrerequisiteCheckbox, not a substitute condition on each unit.
			var options=init.World.LobbyInfo.GlobalSettings.LobbyOptions;
			if(info.Mode=="default") options.Remove("tiberiumdamage");
			else if(info.Mode=="on" || info.Mode=="off")
				options["tiberiumdamage"]=new Session.LobbyOptionState { Value=info.Mode=="on" ? "True" : "False" };
			else throw new InvalidOperationException("Unknown probe mode.");
		}
		void Log(string text) => File.AppendAllText(info.ResultPath,text+"\n");
		void ITick.Tick(Actor self)
		{
			ticks++;
			if(ticks==1)
			{
				foreach(var a in self.World.Actors.Where(a=>a.Owner.InternalName=="Multi0" && a.TraitOrDefault<Health>()!=null))
					initial.Add(a,a.Trait<Health>().HP);
				if(initial.Count!=7) throw new InvalidOperationException("Expected seven damage reference actors.");
				initial.Keys.Single(a=>a.Location==new CPos(110,-25)).GrantCondition("hazmatsuits");
				Log("MODE "+info.Mode+" actual="+self.World.LobbyInfo.GlobalSettings.OptionOrDefault("tiberiumdamage",false));
			}
			if(ticks!=200) return;
			var layer=self.Trait<IResourceLayer>();
			foreach(var pair in initial)
			{
				var a=pair.Key;var hp=a.Trait<Health>().HP;
				var exposed=a.Location==new CPos(104,-25) || a.Location==new CPos(106,-25);
				if(exposed && layer.GetResource(a.Location).Type==null) throw new InvalidOperationException("Resource missing under infantry.");
				var shouldLoseHealth=info.Mode=="on" && exposed;
				Log(a.Info.Name+" at="+a.Location+" HP "+pair.Value+" -> "+hp+" shouldDamage="+shouldLoseHealth);
				if(shouldLoseHealth ? hp>=pair.Value : hp!=pair.Value) throw new InvalidOperationException("Unexpected Tiberium damage/immunity result.");
			}
			Log("PASS: "+info.Mode+" green/blue infantry, clear ground, hazmat, cyborg, Scrin and vehicle checked.");
		}
	}
}
