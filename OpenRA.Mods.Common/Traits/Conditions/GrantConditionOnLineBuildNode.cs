#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition if this becomes a LineBuild corner segment.")]
	public class GrantConditionOnLineBuildNodeInfo : ITraitInfo, Requires<LineBuildInfo>, Requires<ConditionManagerInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Wall type for connections")]
		public readonly string Type = "wall";

		public object Create(ActorInitializer init) { return new GrantConditionOnLineBuildNode(init.Self, this); }
	}

	public class GrantConditionOnLineBuildNode : INotifyLineBuildSegmentsChanged
	{
		readonly Actor self;
		readonly GrantConditionOnLineBuildNodeInfo info;
		readonly ConditionManager conditionManager;

		int conditionToken = ConditionManager.InvalidConditionToken;
		bool wasNode;

		public GrantConditionOnLineBuildNode(Actor self, GrantConditionOnLineBuildNodeInfo info)
		{
			this.self = self;
			this.info = info;

			conditionManager = self.Trait<ConditionManager>();
		}

		void CheckCondition()
		{
			var adjacentActors = CVec.Directions.SelectMany(dir =>
				self.World.ActorMap.GetActorsAt(self.Location + dir));

			var adjacent = 0;

			foreach (var a in adjacentActors)
			{
				CVec facing;
				var wc = a.TraitsImplementing<IWallConnector>().FirstEnabledTraitOrDefault();
				if (wc == null || !wc.AdjacentWallCanConnect(a, self.Location, info.Type, out facing))
					continue;

				if (facing.Y > 0)
					adjacent |= 1;
				else if (facing.X < 0)
					adjacent |= 2;
				else if (facing.Y < 0)
					adjacent |= 4;
				else if (facing.X > 0)
					adjacent |= 8;
			}

			var isNode = adjacent != 5 && adjacent != 10;
			if (isNode && !wasNode && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
			else if (!isNode && wasNode && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

			wasNode = isNode;
		}

		void INotifyLineBuildSegmentsChanged.SegmentAdded(Actor self, Actor segment)
		{
			CheckCondition();
		}

		void INotifyLineBuildSegmentsChanged.SegmentRemoved(Actor self, Actor segment)
		{
			CheckCondition();
		}
	}
}
