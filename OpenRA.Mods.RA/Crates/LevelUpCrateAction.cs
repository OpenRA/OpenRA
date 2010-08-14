#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Mods.RA
{
	class LevelUpCrateActionInfo : CrateActionInfo
	{
		public override object Create(ActorInitializer init) { return new LevelUpCrateAction(init.self, this); }
	}

	class LevelUpCrateAction : CrateAction
	{
		public LevelUpCrateAction(Actor self, LevelUpCrateActionInfo info)
			: base(self,info) {}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var gainsExperience = collector.TraitOrDefault<GainsExperience>();
				if (gainsExperience != null)
					gainsExperience.GiveOneLevel();
			});

			base.Activate(collector);
		}
	}
}
