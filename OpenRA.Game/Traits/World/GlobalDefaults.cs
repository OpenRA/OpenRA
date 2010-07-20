#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

// THIS STUFF NEEDS TO GO DIE IN A FIRE.

namespace OpenRA.Traits
{
	public class GlobalDefaultsInfo : TraitInfo<GlobalDefaults>
	{	
		/* Repair & Refit */
		public readonly float RefundPercent = 0.5f;
		public readonly float RepairPercent = 0.2f;
		public readonly float RepairRate = 0.016f;
		public readonly int RepairStep = 7;
		
		/* Audo/Visual Map Controls */
		public readonly float ConditionRed = 0.25f;
		public readonly float ConditionYellow = 0.5f;
	}

	public class GlobalDefaults {}
}
