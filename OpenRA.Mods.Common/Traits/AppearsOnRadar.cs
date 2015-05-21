#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
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

		public IEnumerable<Pair<CPos, Color>> RadarSignatureCells(Actor self)
		{
			var mod = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
			var color = mod != null ? mod.RadarColorOverride(self) : self.Owner.Color.RGB;

			if (info.UseLocation)
				return new[] { Pair.New(self.Location, color) };
			else
				return self.OccupiesSpace.OccupiedCells().Select(c => Pair.New(c.First, color));
		}
	}
}