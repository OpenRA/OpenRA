#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	class SandwormInfo : Requires<RenderUnitInfo>, Requires<MobileInfo>, IOccupySpaceInfo
	{
		readonly public string WormSignNotification = "WormSign";
		
		public object Create(ActorInitializer init) { return new Sandworm(this); }
	}

	class Sandworm
	{
		public Sandworm(SandwormInfo info)
		{
		}
	}
}
