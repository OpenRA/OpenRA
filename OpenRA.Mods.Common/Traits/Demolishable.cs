#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
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

	public class Demolishable : ConditionalTrait<DemolishableInfo>, IDemolishable, ITick
	{
		class DemolishAction
		{
			public readonly Actor Saboteur;
			public readonly int Token;
			public int Delay;

			public DemolishAction(Actor saboteur, int delay, int token)
			{
				Saboteur = saboteur;
				Delay = delay;
				Token = token;
			}
		}

		List<DemolishAction> actions = new List<DemolishAction>();
		List<DemolishAction> removeActions = new List<DemolishAction>();

		public Demolishable(DemolishableInfo info)
			: base(info) { }

		bool IDemolishable.IsValidTarget(Actor self, Actor saboteur)
		{
			return !IsTraitDisabled;
		}

		void IDemolishable.Demolish(Actor self, Actor saboteur, int delay)
		{
			if (IsTraitDisabled)
				return;

			var token = self.GrantCondition(Info.Condition);
			actions.Add(new DemolishAction(saboteur, delay, token));
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			foreach (var a in actions)
			{
				if (a.Delay-- <= 0)
				{
					var modifiers = self.TraitsImplementing<IDamageModifier>()
						.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
						.Select(t => t.GetDamageModifier(self, null));

					if (Util.ApplyPercentageModifiers(100, modifiers) > 0)
						self.Kill(a.Saboteur);
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
