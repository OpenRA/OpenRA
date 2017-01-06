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
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition above a certain altitude.")]
	public class GrantConditionAboveAltitudeInfo : ITraitInfo
	{
		[GrantedConditionReference, FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public readonly int MinAltitude = 1;

		public object Create(ActorInitializer init) { return new GrantConditionAboveAltitude(init, this); }
	}

	public class GrantConditionAboveAltitude : ITick, INotifyAddedToWorld, INotifyCreated, INotifyRemovedFromWorld
	{
		readonly GrantConditionAboveAltitudeInfo info;

		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionAboveAltitude(ActorInitializer init, GrantConditionAboveAltitudeInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			if (altitude.Length >= info.MinAltitude)
			{
				token = manager.GrantCondition(self, info.Condition);
			}
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (token != ConditionManager.InvalidConditionToken)
				token = manager.RevokeCondition(self, token);
		}

		void ITick.Tick(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length >= info.MinAltitude)
			{
				if (token == ConditionManager.InvalidConditionToken)
					token = manager.GrantCondition(self, info.Condition);
			}
			else
			{
				if (token != ConditionManager.InvalidConditionToken)
					token = manager.RevokeCondition(self, token);
			}
		}
	}
}
