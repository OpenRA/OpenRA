#region Copyright & License Information
/*
  * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
#endregion

using System;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can accept \"plug\" actors in defined cells. Use Pluggable@n for > 1")]
	public class PluggableInfo : TraitInfo<Pluggable>
	{
		[Desc("Which cell does this plug belong in? Relative to the top left cell of the pluggable structure.")]
		public readonly CVec CellOffset = new CVec();

		[Desc("Plug types supported for this cell.")]
		public readonly string[] PlugTypes = { };
	}

	public class Pluggable { }
}
