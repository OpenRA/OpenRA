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
	public enum Trigger { OnIdle, OnDamaged, OnKilled, OnProduction, OnPlayerWon, OnPlayerLost, OnObjectiveAdded, OnObjectiveCompleted, OnObjectiveFailed };

	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : TraitInfo<ScriptTriggers> { }

	public sealed class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, INotifyObjectivesUpdated, IDisposable
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
			{
				var a = self.ToLuaValue(f.Second);
				f.First.Call(a).Dispose();
				a.Dispose();
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			foreach (var f in Triggers[Trigger.OnDamaged])
			{
				var a = self.ToLuaValue(f.Second);
				var b = e.Attacker.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Run lua callbacks
			foreach (var f in Triggers[Trigger.OnKilled])
			{
				var a = self.ToLuaValue(f.Second);
				var b = e.Attacker.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}

			// Run any internally bound callbacks
			OnKilledInternal(self);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			foreach (var f in Triggers[Trigger.OnProduction])
			{
				var a = self.ToLuaValue(f.Second);
				var b = other.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}
		}

		public void OnPlayerWon(Player player)
		{
			foreach (var f in Triggers[Trigger.OnPlayerWon])
			{
				var a = player.ToLuaValue(f.Second);
				f.First.Call(a).Dispose();
				a.Dispose();
			}
		}

		public void OnPlayerLost(Player player)
		{
			foreach (var f in Triggers[Trigger.OnPlayerLost])
			{
				var a = player.ToLuaValue(f.Second);
				f.First.Call(a).Dispose();
				a.Dispose();
			}
		}

		public void OnObjectiveAdded(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveAdded])
			{
				var a = player.ToLuaValue(f.Second);
				var b = id.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}
		}

		public void OnObjectiveCompleted(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveCompleted])
			{
				var a = player.ToLuaValue(f.Second);
				var b = id.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}
		}

		public void OnObjectiveFailed(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveFailed])
			{
				var a = player.ToLuaValue(f.Second);
				var b = id.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}
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
