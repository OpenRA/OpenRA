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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor gain experience in a set periodic time.")]
	public class ExperienceTricklerInfo : PausableConditionalTraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("Number of ticks to wait between giving experience.")]
		public readonly int Interval = 50;

		[Desc("Number of ticks to wait before giving first experience.")]
		public readonly int InitialDelay = 0;

		[Desc("Amount of experience to give each time.")]
		public readonly int Amount = 15;

		public override object Create(ActorInitializer init) { return new ExperienceTrickler(init.Self, this); }
	}

	public class ExperienceTrickler : PausableConditionalTrait<ExperienceTricklerInfo>, ITick, ISync
	{
		readonly ExperienceTricklerInfo info;
		readonly GainsExperience gainsExperience;

		[Sync]
		int ticks;

		public ExperienceTrickler(Actor self, ExperienceTricklerInfo info)
			: base(info)
		{
			this.info = info;
			ticks = info.InitialDelay;
			gainsExperience = self.Trait<GainsExperience>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				ticks = info.Interval;

			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = info.Interval;
				gainsExperience.GiveExperience(info.Amount);
			}
		}
	}
}
