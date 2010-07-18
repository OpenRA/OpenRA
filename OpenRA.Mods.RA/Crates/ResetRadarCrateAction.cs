#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HideMapCrateActionInfo : CrateActionInfo
	{
		public override object Create(ActorInitializer init) { return new HideMapCrateAction(init.self, this); }
	}

	class HideMapCrateAction : CrateAction
	{
		public HideMapCrateAction(Actor self, HideMapCrateActionInfo info)
			: base(self, info) {}

		public override void Activate(Actor collector)
		{
			base.Activate(collector);
			if (collector.Owner == collector.World.LocalPlayer)
				collector.World.WorldActor.traits.Get<Shroud>().ResetExploration();
		}
	}
}
