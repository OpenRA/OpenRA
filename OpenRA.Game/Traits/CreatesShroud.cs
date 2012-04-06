#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits
{
	public class CreatesShroudInfo : ITraitInfo
	{
		public readonly int Range = 0;
		public object Create(ActorInitializer init) { return new CreatesShroud(this); }
	}

	public class CreatesShroud : ITick
	{
		CreatesShroudInfo Info;

		public CreatesShroud(CreatesShroudInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{
			if (!self.IsDisabled())
				self.World.WorldActor.Trait<Shroud>().HideActor(self, Info.Range);
		}
	}
}