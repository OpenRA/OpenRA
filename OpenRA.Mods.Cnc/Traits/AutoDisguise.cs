#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
//// using System.Text;
//// using System.Threading.Tasks;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class AutoDisguiseInfo : ConditionalTraitInfo, Requires<DisguiseInfo>
	{
		[Desc("Measured in game ticks.",
			"How long to wait after spawning to check for a target.")]
		public readonly int InitialDelay = 100;

		[Desc("Measured in game ticks.",
			"How long to wait before checking for a new disguise if disguise breaks.")]
		public readonly int DisguisingDelay = 150;

		[Desc("-1 to scan whole map, 0 or larger to limit scan radius.")]
		public readonly int ScanRadius = -1;
		
		[GrantedConditionReference]
		[Desc("The condition to grant to self while Auto Disguising")]
		public readonly string AutoDisguisedCondition = null;

		[Desc("Do you want this actor to disguise while it is actively doing something or idle?",
			"valid input true or false")]
		public readonly bool ActiveDisguise = false;

		public override object Create(ActorInitializer init) { return new AutoDisguise(this); }
	}

	public class AutoDisguise : ConditionalTrait<AutoDisguiseInfo>, INotifyCreated, IResolveOrder, INotifyAttack, 
		INotifyDamage, ITick, ISync, INotifyIdle
	{
		[Sync] int timeRemaining;

		ConditionManager conditionManager;
		int autoDisguisingToken = ConditionManager.InvalidConditionToken;

		public AutoDisguise(AutoDisguiseInfo info)
			: base(info)
		{
			timeRemaining = info.InitialDelay;
		}

		public bool CanTargetActor(Actor self, Actor target)
		{
			var disguisehash = new HashSet<string>() { "Disguise" };
			var disguieinfo = self.TraitOrDefault<Disguise>().Info;
			if (disguieinfo != null)
			{
				return disguisehash.Overlaps(target.GetEnabledTargetTypes()) &&
						(!disguieinfo.ValidTargets.Any() || (disguieinfo.ValidTargets.Any() &&
						disguieinfo.ValidTargets.Overlaps(target.GetEnabledTargetTypes())));
			}
			else
			{
				return false;
			}
		}

		public Actor ScanForTarget(Actor self)
		{
			if (Info.ScanRadius > 0)
			{
				return ChooseTarget(self, WDist.FromCells(Info.ScanRadius));
			}
			else if (Info.ScanRadius < 0)
			{
				return ChooseTarget(self, WDist.MaxValue);
			}
			else
			{
				return null;
			}
		}

		Actor ChooseTarget(Actor self, WDist range)
		{
			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, range);
			var validActorsInRange = new List<Actor>();

			foreach (Actor possibleTarget in actorsInRange)
			{
				if (self == possibleTarget)
					continue;
				var cantarget = CanTargetActor(self, possibleTarget);
				if (!cantarget)
					continue;

				validActorsInRange.Add(possibleTarget);
			}

			if (validActorsInRange != null && validActorsInRange.Any())
			{
				var target = validActorsInRange.ClosestTo(self);
				if (target.IsInWorld)
				{
					return target;
				}
				else
				{
					return null;
				}
			}

			return null;
		}

		public void ScanAndDisguise(Actor self)
		{
			var disguisingself = self.TraitOrDefault<Disguise>();

			if (disguisingself != null && !disguisingself.Disguised)
			{
				if (timeRemaining <= 0)
				{
					var target = ScanForTarget(self);

					if (target != null)
					{
						disguisingself.DisguiseAs(target);
					}

					timeRemaining = Info.DisguisingDelay;
				}
			}

			HandleCondition(self);
		}

		void HandleCondition(Actor self)
		{
			var disguisingself = self.TraitOrDefault<Disguise>();
			if (disguisingself != null && conditionManager != null)
			{
				if (disguisingself.Disguised && autoDisguisingToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.AutoDisguisedCondition))
					autoDisguisingToken = conditionManager.GrantCondition(self, Info.AutoDisguisedCondition);
				else if (!disguisingself.Disguised && autoDisguisingToken != ConditionManager.InvalidConditionToken)
					autoDisguisingToken = conditionManager.RevokeCondition(self, autoDisguisingToken);
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value == 0)
				return;

			if (e.Damage.Value > 0 && !Info.ActiveDisguise)
				timeRemaining = Info.DisguisingDelay;
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (!Info.ActiveDisguise)
				timeRemaining = Info.DisguisingDelay;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			// I have obsolutely no clue what to put in here, HELP!
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (timeRemaining > 0)
				timeRemaining--;

			if (Info.ActiveDisguise)
				ScanAndDisguise(self);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (!IsTraitDisabled)
			{
				if (!Info.ActiveDisguise)
					ScanAndDisguise(self);
			}
		}
	}
}
