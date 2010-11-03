#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using System.Linq;
using System.Collections.Generic;

namespace OpenRA.Traits
{
	public class TargetableInfo : ITraitInfo
	{
		public readonly string[] TargetTypes = {};
		public virtual object Create( ActorInitializer init ) { return new Targetable(this); }
	}

	public class Targetable
	{
		protected TargetableInfo Info;
		public Targetable(TargetableInfo info)
		{
			Info = info;
		}
		
		public virtual string[] TargetTypes
		{
			get { return Info.TargetTypes;}
		}

		public virtual IEnumerable<int2> TargetableSquares( Actor self )
		{
			yield return self.Location;
		}
	}
}
