#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA
{
	class LevelUpCrateActionInfo : CrateActionInfo
	{
		public readonly int Levels = 1;

		public override object Create(ActorInitializer init) { return new LevelUpCrateAction(init.self, this); }
	}

	class LevelUpCrateAction : CrateAction
	{
		public LevelUpCrateAction(Actor self, LevelUpCrateActionInfo info)
			: base(self,info) {}

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

			base.Activate(collector);
		}
	}
}
