using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can recieve attachments through AttachToTargetWarheads.")]
	public class DelayedWeaponAttachableInfo : ITraitInfo
	{
		[Desc("Types of actors that it can attach to, as long as the type also exists in the Attachable Type: trait.")]
		public readonly HashSet<string> AttachableTypes = new HashSet<string> { "bomb" };

		[Desc("Defines how many objects can be attached at any given time.")]
		public readonly int AttachLimit = 1;
		
		public object Create(ActorInitializer init) { return new DelayedWeaponAttachable(this); }
	}

	public class DelayedWeaponAttachable : ITick, INotifyKilled
	{
		public readonly DelayedWeaponAttachableInfo Info;

		public DelayedWeaponAttachable(DelayedWeaponAttachableInfo info) { Info = info; }

		private HashSet<DelayedWeaponTrigger> container = new HashSet<DelayedWeaponTrigger>();
		
		public void Tick(Actor self)
		{
			foreach (var trigger in container)
			{
				trigger.Tick(self);
			}

			container.RemoveWhere(p => !p.IsValid);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach (var trigger in container)
			{
				if (trigger.DeathTypes.Count > 0 && !e.Damage.DamageTypes.Overlaps(trigger.DeathTypes))
					continue;

				trigger.Activate(self);
			}

			container.RemoveWhere(p => !p.IsValid);
		}

		public bool CanAttach(string type)
		{
			return Info.AttachableTypes.Contains(type) && container.Count < Info.AttachLimit;
		}

		public void Attach(DelayedWeaponTrigger bomb)
		{
			container.Add(bomb);
		}
	}
}
