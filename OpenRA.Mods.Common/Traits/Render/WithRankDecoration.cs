#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithRankDecorationInfo : WithDecorationInfo
	{
		public override object Create(ActorInitializer init) { return new WithRankDecoration(init.Self, this); }
	}

	public class WithRankDecoration : WithDecoration
	{
		public WithRankDecoration(Actor self, WithRankDecorationInfo info) : base(self, info) { }

		protected override void UpgradeLevelChanged(Actor self, int oldLevel, int newLevel)
		{
			Anim.PlayFetchIndex(Info.Sequence, () => newLevel - 1);
		}
	}
}
