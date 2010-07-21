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

namespace OpenRA.Traits
{
	public class UnitInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;

		public object Create( ActorInitializer init ) { return new Unit(); }
	}

	public class Unit : INotifyDamage, IRadarSignature
	{
		[Sync]
		public int Facing;
		[Sync]
		public int Altitude;

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				if (self.Owner == self.World.LocalPlayer)
					Sound.PlayVoice("Lost", self);
		}
		
		public IEnumerable<int2> RadarSignatureCells(Actor self)
		{
			yield return self.Location;
		}
		
		public Color RadarSignatureColor(Actor self)
		{
			return self.Owner.Color;
		}
	}
}
