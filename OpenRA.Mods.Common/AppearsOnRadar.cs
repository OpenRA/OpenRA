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
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class AppearsOnRadarInfo : ITraitInfo
	{
		public readonly bool UseLocation = false;

		public object Create(ActorInitializer init) { return new AppearsOnRadar(this); }
	}

	public class AppearsOnRadar : IRadarSignature
	{
		AppearsOnRadarInfo info;

		public AppearsOnRadar(AppearsOnRadarInfo info) { this.info = info; }

		public IEnumerable<CPos> RadarSignatureCells(Actor self)
		{
			if (info.UseLocation)
				return new CPos[] { self.Location };
			else
				return self.OccupiesSpace.OccupiedCells().Select(c => c.First);
		}

		public Color RadarSignatureColor(Actor self)
		{
			var mod = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
			if (mod != null)
				return mod.RadarColorOverride(self);

			return self.Owner.Color.RGB;
		}
	}
}