#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class AppearsOnRadarInfo : TraitInfo<AppearsOnRadar> {}
	public class AppearsOnRadar : IRadarSignature
	{		
		IOccupySpace Space;

		public IEnumerable<int2> RadarSignatureCells(Actor self)
		{
			if (Space == null)
				Space = self.Trait<IOccupySpace>();
			return Space.OccupiedCells();
		}
		
		public Color RadarSignatureColor(Actor self)
		{
			var mod = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
			if (mod != null)
				return mod.RadarColorOverride(self);
			
			return self.Owner.Color;
		}
	}
}