#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common
{
	public class ContainsCrateInfo : TraitInfo<ContainsCrate> { }

	public class ContainsCrate : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			self.World.AddFrameEndTask(w => w.CreateActor("crate", new TypeDictionary
			{
				new LocationInit(self.Location),
				new OwnerInit(self.World.WorldActor.Owner),
			}));
		}
	}
}
