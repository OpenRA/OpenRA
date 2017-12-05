using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class DelayedWeaponTrigger
	{
		private int triggerTimer;

		private WeaponInfo weaponInfo;

		private Actor attachedBy;

		public bool IsValid { get; private set; } = true;

		public DelayedWeaponTrigger(int triggerTimer, WeaponInfo weaponInfo, Actor attachedBy)
		{
			this.triggerTimer = triggerTimer;
			this.weaponInfo = weaponInfo;
			this.attachedBy = attachedBy;
		}

		public void Tick(Actor attachable)
		{
			triggerTimer--;
			if (!attachable.IsDead && attachable.IsInWorld)
			{
				if (triggerTimer == 0)
				{
					IsValid = false;
					Trigger(attachable);
				}
			}
		}

		private void Trigger(Actor attachable)
		{
			var target = Target.FromPos(attachable.CenterPosition);
			attachable.World.AddFrameEndTask(w => weaponInfo.Impact(target, attachedBy, Enumerable.Empty<int>()));
		}
	}
}
