#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	public class LuaScriptEventsInfo : TraitInfo<LuaScriptEvents> { }

	public class LuaScriptEvents : INotifyKilled, INotifyAddedToWorld, INotifyRemovedFromWorld,
		INotifyCapture, INotifyDamage, INotifyIdle, INotifyProduction
	{
		public event Action<Actor, AttackInfo> OnKilled = (self, e) => { };
		public event Action<Actor> OnAddedToWorld = self => { };
		public event Action<Actor> OnRemovedFromWorld = self => { };
		public event Action<Actor, Actor, Player, Player> OnCaptured = (self, captor, oldOwner, newOwner) => { };
		public event Action<Actor, AttackInfo> OnDamaged = (self, e) => { };
		public event Action<Actor> OnIdle = self => { };
		public event Action<Actor, Actor, CPos> OnProduced = (self, other, exit) => { };

		public void Killed(Actor self, AttackInfo e)
		{
			OnKilled(self, e);
		}

		public void AddedToWorld(Actor self)
		{
			OnAddedToWorld(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			OnRemovedFromWorld(self);
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			OnCaptured(self, captor, oldOwner, newOwner);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			OnDamaged(self, e);
		}

		public void TickIdle(Actor self)
		{
			OnIdle(self);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			OnProduced(self, other, exit);
		}
	}
}
