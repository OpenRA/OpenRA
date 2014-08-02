#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.RA
{
	[Desc("Gives experience levels to the collector.")]
	class LevelUpCrateActionInfo : CrateActionInfo
	{
		[Desc("Number of experience levels to give.")]
		public readonly int Levels = 1;

		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WRange Range = new WRange(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new LevelUpCrateAction(init.self, this); }
	}

	class LevelUpCrateAction : CrateAction
	{
		LevelUpCrateActionInfo Info;

		public LevelUpCrateAction(Actor self, LevelUpCrateActionInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override int GetSelectionShares(Actor collector)
		{
			var ge = collector.TraitOrDefault<GainsExperience>();
			return ge != null && ge.CanGainLevel ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var gainsExperience = collector.TraitOrDefault<GainsExperience>();
				if (gainsExperience != null)
					gainsExperience.GiveLevels(((LevelUpCrateActionInfo)info).Levels);
			});

			var inRange = self.World.FindActorsInCircle(self.CenterPosition, Info.Range);
			inRange = inRange.Where(a =>
				(a.Owner == collector.Owner) &&
				(a != collector) &&
				(a.TraitOrDefault<GainsExperience>() != null) &&
				(a.TraitOrDefault<GainsExperience>().CanGainLevel));
			if (inRange.Any())
			{
				if (Info.MaxExtraCollectors > -1)
					inRange = inRange.Take(Info.MaxExtraCollectors);

				if (inRange.Any())
					foreach (Actor actor in inRange)
					{
						actor.World.AddFrameEndTask(w =>
						{
							var gainsExperience = actor.TraitOrDefault<GainsExperience>();
							if (gainsExperience != null)
								gainsExperience.GiveLevels(((LevelUpCrateActionInfo)info).Levels);
						});
					}
			}

			base.Activate(collector);
		}
	}
}
