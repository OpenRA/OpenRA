#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	public interface IAcceptOre
	{
		void OnDock(Actor harv, DeliverResources dockOrder);
		void GiveOre(int amount);
		void LinkHarvester(Actor self, Actor harv);
		void UnlinkHarvester(Actor self, Actor harv);
		int2 DeliverOffset { get; }
	}
	
	public interface IAcceptOreDockAction
	{
		void OnDock(Actor self, Actor harv, DeliverResources dockOrder);
	}
}
