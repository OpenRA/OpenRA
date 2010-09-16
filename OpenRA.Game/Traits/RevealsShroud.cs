#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Traits
{
	class RevealsShroudInfo : ITraitInfo
	{
		public readonly int Range = 0;	
		public object Create(ActorInitializer init) { return new RevealsShroud(this); }
	}

	class RevealsShroud : ITick
	{
		RevealsShroudInfo Info;
		int2 previousLocation;
		
		public RevealsShroud(RevealsShroudInfo info)
		{
			Info = info;
		}
		
		public void Tick(Actor self)
		{	
			if (previousLocation != self.Location)
			{
				previousLocation = self.Location;
				self.World.WorldActor.Trait<Shroud>().UpdateActor(self);
			}
		}
		
		public int RevealRange { get { return Info.Range; } }
	}
}
