#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	//TODO: Add functionality like a customizable Height that is compared to projectile altitude
	[Desc("This actor blocks bullets and missiles without 'High' property.")]
	public class BlocksBulletsInfo : TraitInfo<BlocksBullets> { }
	public class BlocksBullets : IBlocksBullets { }
}
