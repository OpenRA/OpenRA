using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can recieve attachments through AttachToTargetWarheads.")]
	public class DelayedWeaponAttachableInfo : ConditionalTraitInfo
	{
		[Desc("Type of actors that can attach to it.")]
		public readonly string Type = "bomb";

		[Desc("Defines how many objects can be attached at any given time.")]
		public readonly int AttachLimit = 1;

		[Desc("Show a bar indicating the progress until triggering the with the smallest remaining time.")]
		public readonly bool ShowProgressBar = true;

		[GrantedConditionReference, FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public readonly Color ProgressBarColor = Color.DarkRed;

		public override object Create(ActorInitializer init) { return new DelayedWeaponAttachable(init.Self, this); }
	}

	public class DelayedWeaponAttachable : ConditionalTrait<DelayedWeaponAttachableInfo>, ITick, INotifyKilled, ISelectionBar, INotifyCreated
	{
		public HashSet<DelayedWeaponTrigger> Container { get; private set; } = new HashSet<DelayedWeaponTrigger>();

		private Actor self;

		private HashSet<Actor> detectors = new HashSet<Actor>();

		private int token = ConditionManager.InvalidConditionToken;

		private bool IsEnabled { get { return token != ConditionManager.InvalidConditionToken; } }

		private ConditionManager manager;

		public bool DisplayWhenEmpty => false;

		public DelayedWeaponAttachable(Actor self, DelayedWeaponAttachableInfo info) : base(info)
		{
			this.self = self;
		}

		public void Tick(Actor self)
		{
			if (!IsTraitDisabled)
			{ 
				foreach (var trigger in Container)
				{
					trigger.Tick(self);
				}

				Container.RemoveWhere(p => !p.IsValid);
				if (token != ConditionManager.InvalidConditionToken && !Container.Any())
				{
					token = manager.RevokeCondition(self, token);
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!IsTraitDisabled)
			{
				foreach (var trigger in Container)
				{
					if (trigger.DeathTypes.Count > 0 && !e.Damage.DamageTypes.Overlaps(trigger.DeathTypes))
						continue;

					trigger.Activate(self);
				}

				Container.RemoveWhere(p => !p.IsValid);
			}
		}

		public bool CanAttach(string type)
		{
			return Info.Type == type && Container.Count < Info.AttachLimit;
		}

		public void Attach(DelayedWeaponTrigger trigger)
		{
			if (token == ConditionManager.InvalidConditionToken)
				token = manager.GrantCondition(self, Info.Condition);

			Container.Add(trigger);
		}

		public float GetValue()
		{
			var value = 0f;

			if (!Info.ShowProgressBar || Container.Count == 0)
				return value;
			var smallestTrigger = Container.Where(b => b.AttachedBy.Owner.IsAlliedWith(self.World.LocalPlayer) || detectors.Any(d => d.Owner.IsAlliedWith(self.World.LocalPlayer))).MinByOrDefault(t => t.RemainingTime);
			if (smallestTrigger == null)
				return value;
			return smallestTrigger.RemainingTime * 1.0f / smallestTrigger.TriggerTime;
		}

		public Color GetColor()
		{
			return Info.ProgressBarColor;
		}

		public void AddDetector(Actor detector)
		{
			detectors.Add(detector);
		}
		
		public void RemoveDetector(Actor detector)
		{
			if (detectors.Contains(detector))
				detectors.Remove(detector);
		}

		void INotifyCreated.Created(Actor self)
		{ 
			manager = self.Trait<ConditionManager>();
		}
	}
}
