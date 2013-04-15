#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeHutInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new BridgeHut(init); }
	}

	class BridgeHut
	{
		public Bridge bridge;

		public BridgeHut(ActorInitializer init)
		{
			bridge = init.Get<ParentActorInit>().value.Trait<Bridge>();
		}

		public void Repair(Actor repairer)
		{
			bridge.Repair(repairer, true, true);
		}

		public DamageState BridgeDamageState { get { return bridge.AggregateDamageState(); } }
	}
}
