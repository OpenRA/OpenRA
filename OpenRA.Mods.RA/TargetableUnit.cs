#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TargetableUnitInfo : ITraitInfo
	{
		public readonly string[] TargetTypes = { };
		public virtual object Create( ActorInitializer init ) { return new TargetableUnit<TargetableUnitInfo>( this ); }
	}

	public class TargetableUnit<Info> : ITargetable
		where Info : TargetableUnitInfo
	{
		protected readonly Info info;
		public TargetableUnit( Info info )
		{
			this.info = info;
		}

		public virtual string[] TargetTypes { get { return info.TargetTypes; } }

		public virtual IEnumerable<int2> TargetableSquares( Actor self )
		{
			yield return Util.CellContaining( self.CenterLocation );
		}
	}
}
