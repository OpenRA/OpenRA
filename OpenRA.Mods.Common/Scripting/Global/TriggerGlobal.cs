#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using Eluant;
using OpenRA.Effects;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Trigger")]
	public class TriggerGlobal : ScriptGlobal
	{
		public TriggerGlobal(ScriptContext context)
			: base(context) { }

		public static ScriptTriggers GetScriptTriggers(Actor a)
		{
			var events = a.TraitOrDefault<ScriptTriggers>();
			if (events == null)
				throw new LuaException($"Actor '{a.Info.Name}' requires the ScriptTriggers trait before attaching a trigger");

			return events;
		}

		[Desc("Call a function after a specified delay. The callback function will be called as func().")]
		public void AfterDelay(int delay, LuaFunction func)
		{
			var f = (LuaFunction)func.CopyReference();
			Action doCall = () =>
			{
				try
				{
					using (f)
						f.Call().Dispose();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			Context.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, doCall)));
		}

		[Desc("Call a function for each passenger when it enters a transport. " +
			"The callback function will be called as func(Actor transport, Actor passenger).")]
		public void OnPassengerEntered(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnPassengerEntered, func, Context);
		}

		[Desc("Call a function for each passenger when it exits a transport. " +
			"The callback function will be called as func(Actor transport, Actor passenger).")]
		public void OnPassengerExited(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnPassengerExited, func, Context);
		}

		[Desc("Call a function each tick that the actor is idle. " +
			"The callback function will be called as func(Actor self).")]
		public void OnIdle(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnIdle, func, Context);
		}

		[Desc("Call a function when the actor is damaged. The callback " +
			"function will be called as func(Actor self, Actor attacker, int damage).")]
		public void OnDamaged(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnDamaged, func, Context);
		}

		[Desc("Call a function when the actor is killed. The callback " +
			"function will be called as func(Actor self, Actor killer).")]
		public void OnKilled(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnKilled, func, Context);
		}

		[Desc("Call a function when all of the actors in a group are killed. The callback " +
			"function will be called as func().")]
		public void OnAllKilled(Actor[] actors, LuaFunction func)
		{
			var group = actors.ToList();
			var f = (LuaFunction)func.CopyReference();
			Action<Actor> onMemberKilled = m =>
			{
				try
				{
					group.Remove(m);
					if (group.Count == 0)
						using (f)
							f.Call();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			foreach (var a in group)
				GetScriptTriggers(a).OnKilledInternal += onMemberKilled;
		}

		[Desc("Call a function when one of the actors in a group is killed. The callback " +
			"function will be called as func(Actor killed).")]
		public void OnAnyKilled(Actor[] actors, LuaFunction func)
		{
			var called = false;
			var f = (LuaFunction)func.CopyReference();
			Action<Actor> onMemberKilled = m =>
			{
				try
				{
					if (called)
						return;

					using (f)
					using (var killed = m.ToLuaValue(Context))
						f.Call(killed).Dispose();

					called = true;
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			foreach (var a in actors)
				GetScriptTriggers(a).OnKilledInternal += onMemberKilled;
		}

		[Desc("Call a function when this actor produces another actor. " +
			"The callback function will be called as func(Actor producer, Actor produced).")]
		public void OnProduction(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnProduction, func, Context);
		}

		[Desc("Call a function when any actor produces another actor. The callback " +
			"function will be called as func(Actor producer, Actor produced, string productionType).")]
		public void OnAnyProduction(LuaFunction func)
		{
			GetScriptTriggers(Context.World.WorldActor).RegisterCallback(Trigger.OnOtherProduction, func, Context);
		}

		[Desc("Call a function when this player completes all primary objectives. " +
			"The callback function will be called as func(Player player).")]
		public void OnPlayerWon(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnPlayerWon, func, Context);
		}

		[Desc("Call a function when this player fails any primary objective. " +
			"The callback function will be called as func(Player player).")]
		public void OnPlayerLost(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnPlayerLost, func, Context);
		}

		[Desc("Call a function when this player is assigned a new objective. " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveAdded(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveAdded, func, Context);
		}

		[Desc("Call a function when this player completes an objective. " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveCompleted(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveCompleted, func, Context);
		}

		[Desc("Call a function when this player fails an objective. " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveFailed(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveFailed, func, Context);
		}

		[Desc("Call a function when this actor is added to the world. " +
			"The callback function will be called as func(Actor self).")]
		public void OnAddedToWorld(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnAddedToWorld, func, Context);
		}

		[Desc("Call a function when this actor is removed from the world. " +
			"The callback function will be called as func(Actor self).")]
		public void OnRemovedFromWorld(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnRemovedFromWorld, func, Context);
		}

		[Desc("Call a function when all of the actors in a group have been removed from the world. " +
			"The callback function will be called as func().")]
		public void OnAllRemovedFromWorld(Actor[] actors, LuaFunction func)
		{
			var group = actors.ToList();

			var f = (LuaFunction)func.CopyReference();
			Action<Actor> onMemberRemoved = m =>
			{
				try
				{
					if (!group.Remove(m))
						return;

					if (group.Count == 0)
					{
						// Functions can only be .Call()ed once, so operate on a copy so we can reuse it later
						var temp = (LuaFunction)f.CopyReference();
						using (temp)
							temp.Call().Dispose();
					}
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			Action<Actor> onMemberAdded = m =>
			{
				try
				{
					if (!actors.Contains(m) || group.Contains(m))
						return;

					group.Add(m);
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			foreach (var a in group)
			{
				GetScriptTriggers(a).OnRemovedInternal += onMemberRemoved;
				GetScriptTriggers(a).OnAddedInternal += onMemberAdded;
			}
		}

		[Desc("Call a function when this actor is captured. The callback function " +
			"will be called as func(Actor self, Actor captor, Player oldOwner, Player newOwner).")]
		public void OnCapture(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnCapture, func, Context);
		}

		[Desc("Call a function when this actor is killed or captured. " +
			"The callback function will be called as func().")]
		public void OnKilledOrCaptured(Actor a, LuaFunction func)
		{
			var called = false;

			var f = (LuaFunction)func.CopyReference();
			Action<Actor> onKilledOrCaptured = m =>
			{
				try
				{
					if (called)
						return;

					using (f)
						f.Call().Dispose();

					called = true;
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			GetScriptTriggers(a).OnCapturedInternal += onKilledOrCaptured;
			GetScriptTriggers(a).OnKilledInternal += onKilledOrCaptured;
		}

		[Desc("Call a function when all of the actors in a group have been killed or captured. " +
			"The callback function will be called as func().")]
		public void OnAllKilledOrCaptured(Actor[] actors, LuaFunction func)
		{
			var group = actors.ToList();

			var f = (LuaFunction)func.CopyReference();
			Action<Actor> onMemberKilledOrCaptured = m =>
			{
				try
				{
					if (!group.Remove(m))
						return;

					if (group.Count == 0)
						using (f)
							f.Call().Dispose();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			foreach (var a in group)
			{
				GetScriptTriggers(a).OnCapturedInternal += onMemberKilledOrCaptured;
				GetScriptTriggers(a).OnKilledInternal += onMemberKilledOrCaptured;
			}
		}

		[Desc("Call a function when a ground-based actor enters this cell footprint. " +
			"Returns the trigger id for later removal using RemoveFootprintTrigger(int id). " +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnEnteredFootprint(CPos[] cells, LuaFunction func)
		{
			// We can't easily dispose onEntry, so we'll have to rely on finalization for it.
			var onEntry = (LuaFunction)func.CopyReference();
			var triggerId = 0;
			Action<Actor> invokeEntry = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(Context))
					using (var id = triggerId.ToLuaValue(Context))
						onEntry.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			triggerId = Context.World.ActorMap.AddCellTrigger(cells, invokeEntry, null);

			return triggerId;
		}

		[Desc("Call a function when a ground-based actor leaves this cell footprint. " +
			"Returns the trigger id for later removal using RemoveFootprintTrigger(int id). " +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnExitedFootprint(CPos[] cells, LuaFunction func)
		{
			// We can't easily dispose onExit, so we'll have to rely on finalization for it.
			var onExit = (LuaFunction)func.CopyReference();
			var triggerId = 0;
			Action<Actor> invokeExit = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(Context))
					using (var id = triggerId.ToLuaValue(Context))
						onExit.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			triggerId = Context.World.ActorMap.AddCellTrigger(cells, null, invokeExit);

			return triggerId;
		}

		[Desc("Removes a previously created footprint trigger.")]
		public void RemoveFootprintTrigger(int id)
		{
			Context.World.ActorMap.RemoveCellTrigger(id);
		}

		[Desc("Call a function when an actor enters this range. " +
			"Returns the trigger id for later removal using RemoveProximityTrigger(int id). " +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnEnteredProximityTrigger(WPos pos, WDist range, LuaFunction func)
		{
			// We can't easily dispose onEntry, so we'll have to rely on finalization for it.
			var onEntry = (LuaFunction)func.CopyReference();
			var triggerId = 0;
			Action<Actor> invokeEntry = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(Context))
					using (var id = triggerId.ToLuaValue(Context))
						onEntry.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			triggerId = Context.World.ActorMap.AddProximityTrigger(pos, range, WDist.Zero, invokeEntry, null);

			return triggerId;
		}

		[Desc("Call a function when an actor leaves this range. " +
			"Returns the trigger id for later removal using RemoveProximityTrigger(int id). " +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnExitedProximityTrigger(WPos pos, WDist range, LuaFunction func)
		{
			// We can't easily dispose onExit, so we'll have to rely on finalization for it.
			var onExit = (LuaFunction)func.CopyReference();
			var triggerId = 0;
			Action<Actor> invokeExit = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(Context))
					using (var id = triggerId.ToLuaValue(Context))
						onExit.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					Context.FatalError(e.Message);
				}
			};

			triggerId = Context.World.ActorMap.AddProximityTrigger(pos, range, WDist.Zero, null, invokeExit);

			return triggerId;
		}

		[Desc("Removes a previously created proximity trigger.")]
		public void RemoveProximityTrigger(int id)
		{
			Context.World.ActorMap.RemoveProximityTrigger(id);
		}

		[Desc("Call a function when this actor is infiltrated. The callback function " +
			"will be called as func(Actor self, Actor infiltrator).")]
		public void OnInfiltrated(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnInfiltrated, func, Context);
		}

		[Desc("Call a function when this actor is discovered by an enemy or a player with a Neutral stance. " +
			"The callback function will be called as func(Actor discovered, Player discoverer). " +
			"The player actor needs the 'EnemyWatcher' trait. The actors to discover need the 'AnnounceOnSeen' trait.")]
		public void OnDiscovered(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnDiscovered, func, Context);
		}

		[Desc("Call a function when this player is discovered by an enemy or neutral player. " +
			"The callback function will be called as func(Player discovered, Player discoverer, Actor discoveredActor)." +
			"The player actor needs the 'EnemyWatcher' trait. The actors to discover need the 'AnnounceOnSeen' trait.")]
		public void OnPlayerDiscovered(Player discovered, LuaFunction func)
		{
			GetScriptTriggers(discovered.PlayerActor).RegisterCallback(Trigger.OnPlayerDiscovered, func, Context);
		}

		[Desc("Call a function when this actor is sold. The callback function " +
			"will be called as func(Actor self).")]
		public void OnSold(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnSold, func, Context);
		}

		[Desc("Call a function when the game timer expires. The callback function will be called as func().")]
		public void OnTimerExpired(LuaFunction func)
		{
			GetScriptTriggers(Context.World.WorldActor).RegisterCallback(Trigger.OnTimerExpired, func, Context);
		}

		[Desc("Removes all triggers from this actor. " +
			"Note that the removal will only take effect at the end of a tick, " +
			"so you must not add new triggers at the same time that you are calling this function.")]
		public void ClearAll(Actor a)
		{
			GetScriptTriggers(a).ClearAll();
		}

		[Desc("Removes the specified trigger from this actor. " +
			"Note that the removal will only take effect at the end of a tick, " +
			"so you must not add new triggers at the same time that you are calling this function.")]
		public void Clear(Actor a, string triggerName)
		{
			var trigger = (Trigger)Enum.Parse(typeof(Trigger), triggerName);

			GetScriptTriggers(a).Clear(trigger);
		}
	}
}
