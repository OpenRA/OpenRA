using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class DelayedWeaponTrigger
	{
		public readonly HashSet<string> DeathTypes;

		public readonly int TriggerTime;

		public int RemainingTime { get; private set; }

		private WeaponInfo weaponInfo;

		private Actor attachedBy;		

		public bool IsValid { get; private set; } = true;

		public DelayedWeaponTrigger(int triggerTimer, HashSet<string> deathTypes, WeaponInfo weaponInfo, Actor attachedBy)
		{
			this.TriggerTime = triggerTimer;
			this.RemainingTime = triggerTimer;
			this.DeathTypes = deathTypes;
			this.weaponInfo = weaponInfo;
			this.attachedBy = attachedBy;
		}

		public void Tick(Actor attachable)
		{
			RemainingTime--;
			if (!attachable.IsDead && attachable.IsInWorld)
			{
				if (RemainingTime == 0)
				{
					Activate(attachable);
				}
			}
		}

		public void Activate(Actor attachable)
		{
			IsValid = false;
			var target = Target.FromPos(attachable.CenterPosition);
			attachable.World.AddFrameEndTask(w => weaponInfo.Impact(target, attachedBy, Enumerable.Empty<int>()));
		}
	}
}
