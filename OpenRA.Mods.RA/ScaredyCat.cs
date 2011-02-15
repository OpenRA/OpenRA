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
using OpenRA.Traits.Activities;
using System.Drawing;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA
{
	class ScaredyCatInfo : ITraitInfo
	{
		public readonly int MoveRadius = 2;
		
		public object Create(ActorInitializer init) { return new ScaredyCat(init.self, this); }
	}

	class ScaredyCat : INotifyIdle, INotifyDamage
	{
		readonly ScaredyCatInfo Info;
		bool Panicked = false;
		public ScaredyCat(Actor self, ScaredyCatInfo info)
		{
			Info = info;
		}
		
		public void TickIdle(Actor self)
		{
			if (!Panicked)
				return;
			var target = Util.SubPxVector[self.World.SharedRandom.Next(255)]* Info.MoveRadius / 1024 + self.Location;
			self.Trait<Mobile>().ResolveOrder(self, new Order("Move", self, false) { TargetLocation = target });
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (Panicked)
				return;
			Panicked = true;
			self.Trait<RenderInfantry>().Panicked = true;
		}
	}
}
