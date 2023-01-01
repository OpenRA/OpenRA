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

using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Gives experience levels to the collector.")]
	class LevelUpCrateActionInfo : CrateActionInfo
	{
		[Desc("Number of experience levels to give.")]
		public readonly int Levels = 1;

		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WDist Range = new WDist(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new LevelUpCrateAction(init.Self, this); }
	}

	class LevelUpCrateAction : CrateAction
	{
		readonly Actor self;
		readonly LevelUpCrateActionInfo info;

		public LevelUpCrateAction(Actor self, LevelUpCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
		}

		public override int GetSelectionShares(Actor collector)
		{
			var ge = collector.TraitOrDefault<GainsExperience>();
			return ge != null && ge.CanGainLevel ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			var inRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range).Where(a =>
			{
				// Don't touch the same unit twice
				if (a == collector)
					return false;

				// Only affect the collecting player's units
				// TODO: Also apply to allied units?
				if (a.Owner != collector.Owner)
					return false;

				// Ignore units that can't level up
				var ge = a.TraitOrDefault<GainsExperience>();
				return ge != null && ge.CanGainLevel;
			});

			if (info.MaxExtraCollectors > -1)
				inRange = inRange.Take(info.MaxExtraCollectors);

			foreach (var recipient in inRange.Append(collector))
			{
				recipient.World.AddFrameEndTask(w =>
				{
					if (!recipient.IsDead)
						recipient.TraitOrDefault<GainsExperience>()?.GiveLevels(info.Levels);
				});
			}

			base.Activate(collector);
		}
	}
}
