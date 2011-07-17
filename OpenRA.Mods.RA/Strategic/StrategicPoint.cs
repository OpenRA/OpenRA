#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class StrategicPointInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new StrategicPoint(init.self, this); }
	}

	public class StrategicPoint : INotifyCapture, ITick, ISync
	{
		[Sync] public Player OriginalOwner;
		[Sync] public int TicksOwned = 0;

		public StrategicPointInfo Info;

		public StrategicPoint(Actor self, StrategicPointInfo info)
		{
			Info = info;
			OriginalOwner = self.Owner;
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			TicksOwned = 0;
		}

		public void Tick(Actor self) 
		{
			if (OriginalOwner == self.Owner || self.Owner.WinState != WinState.Undefined) return;
			TicksOwned++;
		}
	}
}
