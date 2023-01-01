#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Handle demolitions from C4 explosives.")]
	public class DemolishableInfo : ConditionalTraitInfo, IDemolishableInfo
	{
		public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return true; }

		[GrantedConditionReference]
		[Desc("Condition to grant during demolition countdown.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new Demolishable(this); }
	}

	public class Demolishable : ConditionalTrait<DemolishableInfo>, IDemolishable, ITick, INotifyOwnerChanged
	{
		class DemolishAction
		{
			public readonly Actor Saboteur;
			public readonly int Token;
			public int Delay;
			public readonly BitSet<DamageType> DamageTypes;

			public DemolishAction(Actor saboteur, int delay, int token, BitSet<DamageType> damageTypes)
			{
				Saboteur = saboteur;
				Delay = delay;
				Token = token;
				DamageTypes = damageTypes;
			}
		}

		readonly List<DemolishAction> actions = new List<DemolishAction>();
		readonly List<DemolishAction> removeActions = new List<DemolishAction>();
		IDamageModifier[] damageModifiers;

		public Demolishable(DemolishableInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			damageModifiers = self.TraitsImplementing<IDamageModifier>()
				.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>()).ToArray();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			damageModifiers = self.TraitsImplementing<IDamageModifier>()
				.Concat(newOwner.PlayerActor.TraitsImplementing<IDamageModifier>()).ToArray();
		}

		bool IDemolishable.IsValidTarget(Actor self, Actor saboteur)
		{
			return !IsTraitDisabled;
		}

		void IDemolishable.Demolish(Actor self, Actor saboteur, int delay, BitSet<DamageType> damageTypes)
		{
			if (IsTraitDisabled)
				return;

			var token = self.GrantCondition(Info.Condition);
			actions.Add(new DemolishAction(saboteur, delay, token, damageTypes));
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || actions.Count == 0)
				return;

			foreach (var a in actions)
			{
				if (a.Delay-- <= 0)
				{
					if (Util.ApplyPercentageModifiers(100, damageModifiers.Select(t => t.GetDamageModifier(self, null))) > 0)
						self.Kill(a.Saboteur, a.DamageTypes);
					else if (a.Token != Actor.InvalidConditionToken)
					{
						self.RevokeCondition(a.Token);
						removeActions.Add(a);
					}
				}
			}

			// Remove expired actions to avoid double-revoking
			foreach (var a in removeActions)
				actions.Remove(a);

			removeActions.Clear();
		}
	}
}
