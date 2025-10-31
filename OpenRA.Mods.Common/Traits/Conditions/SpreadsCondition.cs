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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Any actor with this trait enabled has a chance to affect others with this trait.")]
	public class SpreadsConditionInfo : ConditionalTraitInfo
	{
		[Desc("The chance this actor is going to affect an adjacent actor.")]
		public readonly int Probability = 5;

		[Desc("How far the condition can spread from one actor to another.")]
		public readonly WDist Range = WDist.FromCells(3);

		[GrantedConditionReference]
		[Desc("Condition to grant onto another actor in range with the same trait.")]
		public readonly string SpreadCondition = "spreading";

		[Desc("Time in ticks to wait between spreading further.")]
		public readonly int Delay = 5;

		public override object Create(ActorInitializer init) { return new SpreadsCondition(this); }
	}

	public class SpreadsCondition : ConditionalTrait<SpreadsConditionInfo>, ITick
	{
		readonly SpreadsConditionInfo info;

		int delay;

		public SpreadsCondition(SpreadsConditionInfo info)
			: base(info)
		{
			this.info = info;
			delay = info.Delay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (delay-- > 0)
				return;

			delay = info.Delay;

			if (self.World.SharedRandom.Next(100) > info.Probability)
				return;

			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range)
				.Where(a => a.TraitOrDefault<SpreadsCondition>() != null);

			var target = actorsInRange.RandomOrDefault(self.World.SharedRandom);
			target?.GrantCondition(info.SpreadCondition);
		}
	}
}
