#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
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

	public interface ITechTreeElement
	{
		void PrerequisitesAvailable(string key);
		void PrerequisitesUnavailable(string key);
		void PrerequisitesItemHidden(string key);
		void PrerequisitesItemVisible(string key);
	}

	public interface ITechTreePrerequisite
	{
		IEnumerable<string> ProvidesPrerequisites {get;}
	}

	public interface INotifyResourceClaimLost
	{
		void OnNotifyResourceClaimLost(Actor self, ResourceClaim claim, Actor claimer);
	}

	public interface INotifyParachuteLanded { void OnLanded(); }
	public interface INotifyTransform { void BeforeTransform(Actor self); void OnTransform(Actor self); void AfterTransform(Actor toActor); }
	public interface INotifyAttack { void Attacking(Actor self, Target target, Armament a, Barrel barrel); }
	public interface INotifyChat { bool OnChat(string from, string message); }
}
