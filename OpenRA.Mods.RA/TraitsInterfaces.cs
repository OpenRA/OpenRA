#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IAcceptOre
	{
		void OnDock(Actor harv, DeliverResources dockOrder);
		void GiveOre(int amount);
		bool CanGiveOre(int amount);
		CVec DeliverOffset { get; }
		bool AllowDocking { get; }
	}

	public interface IAcceptOreDockAction
	{
		void OnDock(Actor self, Actor harv, DeliverResources dockOrder);
	}

	public interface INotifyAttack { void Attacking(Actor self, Target target, Armament a, Barrel barrel); }
}
