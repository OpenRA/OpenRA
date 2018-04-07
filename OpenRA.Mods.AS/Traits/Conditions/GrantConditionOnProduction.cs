#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition when this actor produces a specific actor.")]
	public class GrantConditionOnProductionInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[ActorReference]
		[Desc("The actors to grant condition for. If empty, the condition will be granted for all actors.")]
		public readonly HashSet<string> Actors = new HashSet<string>();

		[Desc("Duration of the condition. Leave it at 0 to have it permanently.")]
		public int Duration = 0;

		public object Create(ActorInitializer init) { return new GrantConditionOnProduction(init.Self, this); }
	}

	public class GrantConditionOnProduction : INotifyCreated, INotifyOtherProduction, ITick
	{
		readonly GrantConditionOnProductionInfo info;
		ConditionManager manager;
		int duration = -1;

		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionOnProduction(Actor self, GrantConditionOnProductionInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			token = manager.GrantCondition(self, cond);

			if (info.Duration > 0)
				duration = info.Duration;
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType)
		{
			// Only grant to self, not others.
			if (producer != self)
				return;

			if (!info.Actors.Any() || info.Actors.Contains(produced.Info.Name))
				if (token == ConditionManager.InvalidConditionToken)
					GrantCondition(self, info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (token != ConditionManager.InvalidConditionToken && info.Duration > 0 && --duration < 0)
				token = manager.RevokeCondition(self, token);
		}
	}
}