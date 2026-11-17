using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Refills an ammo pool when the actor's support power finishes charging, without a second reload timer.")]
	public sealed class ReloadAmmoOnSupportPowerChargedInfo : TraitInfo, Requires<AmmoPoolInfo>, Requires<SupportPowerInfo>
	{
		public readonly string AmmoPool = "primary";
		public override object Create(ActorInitializer init) => new ReloadAmmoOnSupportPowerCharged(this);
	}

	public sealed class ReloadAmmoOnSupportPowerCharged : INotifyCreated, INotifySupportPower
	{
		readonly ReloadAmmoOnSupportPowerChargedInfo info;
		AmmoPool ammo;
		public ReloadAmmoOnSupportPowerCharged(ReloadAmmoOnSupportPowerChargedInfo info) { this.info = info; }
		void INotifyCreated.Created(Actor self) { ammo = self.TraitsImplementing<AmmoPool>().Single(a => a.Info.Name == info.AmmoPool); }
		void INotifySupportPower.Charged(Actor self) { ammo.GiveAmmo(self, ammo.Info.Ammo); }
		void INotifySupportPower.Activated(Actor self) { }
	}
}
