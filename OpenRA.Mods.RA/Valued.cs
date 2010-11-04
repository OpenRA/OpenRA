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
	public class ValuedInfo : TraitInfo<Valued>
	{
		public readonly int Cost = 0;
	}

	public class TooltipInfo : TraitInfo<Tooltip>
	{
		public readonly string Description = "";
		public readonly string Name = "";
		public readonly string Icon = null;
		public readonly string[] AlternateName = { };
	}
	
	public class Valued { }
	public class Tooltip { }
}
