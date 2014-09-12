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
		OnObjectiveCompleted, OnObjectiveFailed, OnCapture, OnAddedToWorld, OnRemovedFromWorld };

	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new ScriptTriggers(init.world); }
	}

	public sealed class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, INotifyObjectivesUpdated, INotifyCapture, INotifyAddedToWorld, INotifyRemovedFromWorld, IDisposable
	{
		readonly World world;

		public event Action<Actor> OnKilledInternal = _ => { };
		public event Action<Actor> OnRemovedInternal = _ => { };

		public Dictionary<Trigger, List<Pair<LuaFunction, ScriptContext>>> Triggers = new Dictionary<Trigger, List<Pair<LuaFunction, ScriptContext>>>();

		public ScriptTriggers(World world)
		{
			this.world = world;

			foreach (Trigger t in Enum.GetValues(typeof(Trigger)))
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
				try
				{
					using (var a = self.ToLuaValue(f.Second))
						f.First.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			foreach (var f in Triggers[Trigger.OnDamaged])
			{
				try
				{
					using (var a = self.ToLuaValue(f.Second))
					using (var b = e.Attacker.ToLuaValue(f.Second))
						f.First.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Run Lua callbacks
			foreach (var f in Triggers[Trigger.OnKilled])
			{
				try
				{
					using (var a = self.ToLuaValue(f.Second))
					using (var b = e.Attacker.ToLuaValue(f.Second))
						f.First.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnKilledInternal(self);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			foreach (var f in Triggers[Trigger.OnProduction])
			{
				try
				{
					using (var a = self.ToLuaValue(f.Second))
					using (var b = other.ToLuaValue(f.Second))
						f.First.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnPlayerWon(Player player)
		{
			foreach (var f in Triggers[Trigger.OnPlayerWon])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Second))
						f.First.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnPlayerLost(Player player)
		{
			foreach (var f in Triggers[Trigger.OnPlayerLost])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Second))
						f.First.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveAdded(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveAdded])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Second))
					using (var b = id.ToLuaValue(f.Second))
						f.First.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveCompleted(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveCompleted])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Second))
					using (var b = id.ToLuaValue(f.Second))
						f.First.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveFailed(Player player, int id)
		{
			foreach (var f in Triggers[Trigger.OnObjectiveFailed])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Second))
					using (var b = id.ToLuaValue(f.Second))
						f.First.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			foreach (var f in Triggers[Trigger.OnCapture])
			{
				try
				{
					using (var a = self.ToLuaValue(f.Second))
					using (var b = captor.ToLuaValue(f.Second))
					using (var c = oldOwner.ToLuaValue(f.Second))
					using (var d = newOwner.ToLuaValue(f.Second))
						f.First.Call(a, b, c, d).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void AddedToWorld(Actor self)
		{
			foreach (var f in Triggers[Trigger.OnAddedToWorld])
			{
				try
				{
					using (var a = self.ToLuaValue(f.Second))
						f.First.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void RemovedFromWorld(Actor self)
		{
			// Run Lua callbacks
			foreach (var f in Triggers[Trigger.OnRemovedFromWorld])
			{
				try
				{
					using (var a = self.ToLuaValue(f.Second))
						f.First.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnRemovedInternal(self);
		}

		public void Clear(Trigger trigger)
		{
			world.AddFrameEndTask(w =>
			{
				Triggers[trigger].Select(p => p.First).Do(f => f.Dispose());
				Triggers[trigger].Clear();
			});
		}

		public void ClearAll()
		{
			foreach (Trigger t in Enum.GetValues(typeof(Trigger)))
				Clear(t);
		}

		public void Dispose()
		{
			ClearAll();
		}
	}
}
