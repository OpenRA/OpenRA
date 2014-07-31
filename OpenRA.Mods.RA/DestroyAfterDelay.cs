#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Destroys the actor after a specified number of ticks.")]
	public class DestroyAfterDelayInfo : ITraitInfo
	{
		[Desc("Lifetime of actor in ticks.","0 = Actor destroyed on AddedToWorld")]
		public readonly int Ticks = 0;

		public object Create(ActorInitializer init) { return new DestroyAfterDelay(init.self, this); }
	}

	public class DestroyAfterDelay : INotifyAddedToWorld, ITick
	{
		readonly DestroyAfterDelayInfo info;
		int ticks;

		public DestroyAfterDelay(Actor self, DestroyAfterDelayInfo info)
		{
			this.info = info;
			ticks = info.Ticks;
		}

		public void AddedToWorld(Actor self)
		{
			if (info.Ticks <= 0)
				self.Destroy();
		}

		public void Tick(Actor self)
		{
			// TODO: Add in functionality to use .Kill(self) as well as .Destroy() once a use-case exists.
			if (ticks-- <= 0)
				self.Destroy();
		}
	}
}
