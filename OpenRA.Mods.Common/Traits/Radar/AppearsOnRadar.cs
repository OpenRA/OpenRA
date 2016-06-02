#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Radar
{
	public class AppearsOnRadarInfo : ITraitInfo
	{
		public readonly bool UseLocation = false;

		public object Create(ActorInitializer init) { return new AppearsOnRadar(this); }
	}

	public class AppearsOnRadar : IRadarSignature, INotifyCreated
	{
		readonly AppearsOnRadarInfo info;
		IRadarColorModifier modifier;

		public AppearsOnRadar(AppearsOnRadarInfo info)
		{
			this.info = info;
		}

		public void Created(Actor self)
		{
			modifier = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
		}

		public IEnumerable<Pair<CPos, Color>> RadarSignatureCells(Actor self)
		{
			var color = Game.Settings.Game.UsePlayerStanceColors ? self.Owner.PlayerStanceColor(self) : self.Owner.Color.RGB;
			if (modifier != null)
				color = modifier.RadarColorOverride(self, color);

			if (info.UseLocation)
				return new[] { Pair.New(self.Location, color) };

			return self.OccupiesSpace.OccupiedCells().Select(c => Pair.New(c.First, color));
		}
	}
}