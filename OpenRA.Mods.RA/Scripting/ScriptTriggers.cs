#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Eluant;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{

	public enum Trigger { OnIdle, OnDamaged, OnKilled, OnProduction, OnPlayerWon, OnPlayerLost, OnObjectiveAdded,
		OnObjectiveCompleted, OnObjectiveFailed, OnCapture, OnAddedToWorld, OnRemovedFromWorld, OnRegionEntered, OnRegionLeft };

	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : TraitInfo<ScriptTriggers> { }

	public sealed class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, INotifyObjectivesUpdated, INotifyCapture, INotifyAddedToWorld, INotifyRemovedFromWorld, IDisposable, INotifyRegionTrigger
	{
		public event Action<Actor> OnKilledInternal = _ => {};

		public Dictionary<Trigger, List<Pair<LuaFunction, ScriptContext>>> Triggers = new Dictionary<Trigger, List<Pair<LuaFunction, ScriptContext>>>();

		public ScriptTriggers()
		{
			foreach (var t in Enum.GetValues(typeof(Trigger)).Cast<Trigger>())
				Triggers.Add(t, new List<Pair<LuaFunction, ScriptContext>>());
		}

		public void RegisterCallback(Trigger trigger, LuaFunction func, ScriptContext context)
		{
			Triggers[trigger].Add(Pair.New((LuaFunction)func.CopyReference(), context));
		}

		public void TickIdle(Actor self)
		{
			foreach (var f in Triggers[Trigger.OnIdle])
				using (var a = self.ToLuaValue(f.Second))
					f.First.Call(a).Dispose();
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			foreach (var f in Triggers[Trigger.OnDamaged])
				using (var a = self.ToLuaValue(f.Second))
				using (var b = e.Attacker.ToLuaValue(f.Second))
					f.First.Call(a, b).Dispose();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Run lua callbacks
			foreach (var f in Triggers[Trigger.OnKilled])
				using (var a = self.ToLuaValue(f.Second))
				using (var b = e.Attacker.ToLuaValue(f.Second))
					f.First.Call(a, b).Dispose();

			// Run any internally bound callbacks
			OnKilledInternal(self);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			foreach (var f in Triggers[Trigger.OnProduction])
				using (var a = self.ToLuaValue(f.Second))
				using (var b = other.ToLuaValue(f.Second))
					f.First.Call(a, b).Dispose();
		}

		public void EnteredRegion(Player owner, Actor self, string region)
		{
			foreach (var f in Triggers[Trigger.OnRegionEntered])
			{
				var a = self.ToLuaValue(f.Second);
				var b = owner.ToLuaValue(f.Second);
				var c = region.ToLuaValue(f.Second);
				f.First.Call(a, b, c).Dispose();
				a.Dispose();
				b.Dispose();
				c.Dispose();
			}
		}

		public void LeftRegion(Player owner, Actor self, string region)
		{
			foreach (var f in Triggers[Trigger.OnRegionLeft])
			{
				var a = self.ToLuaValue(f.Second);
				var b = owner.ToLuaValue(f.Second);
				var c = region.ToLuaValue(f.Second);
				f.First.Call(a, b, c).Dispose();
				a.Dispose();
				b.Dispose();
				c.Dispose();
			}
		}

		public void OnPlayerWon(Player player)
		{
			foreach (var f in Triggers[Trigger.OnPlayerWon])
				using (var a = player.ToLuaValue(f.Second))
					f.First.Call(a).Dispose();
		}

		public void OnPlayerLost(Player player)
		{
			foreach (var f in Triggers[Trigger.OnPlayerLost])
				using (var a = player.ToLuaValue(f.Second))
					f.First.Call(a).Dispose();
		}

		public void OnObjectiveAdded(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveAdded])
				using (var a = player.ToLuaValue(f.Second))
				using (var b = id.ToLuaValue(f.Second))
					f.First.Call(a, b).Dispose();
		}

		public void OnObjectiveCompleted(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveCompleted])
				using (var a = player.ToLuaValue(f.Second))
				using (var b = id.ToLuaValue(f.Second))
					f.First.Call(a, b).Dispose();
		}

		public void OnObjectiveFailed(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveFailed])
				using (var a = player.ToLuaValue(f.Second))
				using (var b = id.ToLuaValue(f.Second))
					f.First.Call(a, b).Dispose();
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			foreach (var f in Triggers[Trigger.OnCapture])
				using (var a = self.ToLuaValue(f.Second))
				using (var b = captor.ToLuaValue(f.Second))
				using (var c = oldOwner.ToLuaValue(f.Second))
				using (var d = newOwner.ToLuaValue(f.Second))
					f.First.Call(a, b, c, d).Dispose();
		}

		public void AddedToWorld(Actor self)
		{
			foreach (var f in Triggers[Trigger.OnAddedToWorld])
				using (var a = self.ToLuaValue(f.Second))
					f.First.Call(a).Dispose();
		}

		public void RemovedFromWorld(Actor self)
		{
			foreach (var f in Triggers[Trigger.OnRemovedFromWorld])
				using (var a = self.ToLuaValue(f.Second))
					f.First.Call(a).Dispose();
		}

		public void Clear(Trigger trigger)
		{
			Triggers[trigger].Clear();
		}

		public void ClearAll()
		{
			foreach (var trigger in Triggers)
				trigger.Value.Clear();
		}

		public void Dispose()
		{
			var pairs = Triggers.Values;
			pairs.SelectMany(l => l).Select(p => p.First).Do(f => f.Dispose());
			pairs.Do(l => l.Clear());
		}
	}
}
