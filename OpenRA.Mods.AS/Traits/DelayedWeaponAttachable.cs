using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

		[Desc("Show a bar indicating the progress until triggering the with the smallest remaining time.")]
		public readonly bool ShowProgressBar = true;

		public readonly Color ProgressBarColor = Color.DarkRed;

		public object Create(ActorInitializer init) { return new DelayedWeaponAttachable(init.Self, this); }
	}

	public class DelayedWeaponAttachable : ITick, INotifyKilled, ISelectionBar
	{
		public readonly DelayedWeaponAttachableInfo Info;

		public DelayedWeaponAttachable(Actor self, DelayedWeaponAttachableInfo info) { this.self = self; Info = info; }

		private HashSet<DelayedWeaponTrigger> container = new HashSet<DelayedWeaponTrigger>();

		private Actor self;

		public bool DisplayWhenEmpty => false;

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

		public float GetValue()
		{
			var value = 0f;

			if (!Info.ShowProgressBar || container.Count == 0)
				return value;
			var smallestTrigger = container.Where(b => b.AttachedBy.Owner.IsAlliedWith(self.World.RenderPlayer)).MinByOrDefault(t => t.RemainingTime);
			if (smallestTrigger == null)
				return value;
			return smallestTrigger.RemainingTime * 1.0f / smallestTrigger.TriggerTime;
		}

		public Color GetColor()
		{
			return Info.ProgressBarColor;
		}
	}
}
