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
using OpenRA.Effects;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Trigger")]
	public class TriggerGlobal : ScriptGlobal
	{
		public TriggerGlobal(ScriptContext context) : base(context) { }

		public static ScriptTriggers GetScriptTriggers(Actor a)
		{
			var events = a.TraitOrDefault<ScriptTriggers>();
			if (events == null)
				throw new LuaException("Actor '{0}' requires the ScriptTriggers trait before attaching a trigger".F(a.Info.Name));

			return events;
		}

		[Desc("Call a function after a specified delay. The callback function will be called as func().")]
		public void AfterDelay(int delay, LuaFunction func)
		{
			var f = func.CopyReference() as LuaFunction;

			Action doCall = () =>
			{
				try
				{
					using (f)
						f.Call();
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			context.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, doCall)));
		}

		[Desc("Call a function each tick that the actor is idle. " +
			"The callback function will be called as func(Actor self).")]
		public void OnIdle(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnIdle, func, context);
		}

		[Desc("Call a function when the actor is damaged. The callback " +
			"function will be called as func(Actor self, Actor attacker).")]
		public void OnDamaged(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnDamaged, func, context);
		}

		[Desc("Call a function when the actor is killed. The callback " +
			"function will be called as func(Actor self, Actor killer).")]
		public void OnKilled(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnKilled, func, context);
		}

		[Desc("Call a function when all of the actors in a group are killed. The callback " +
			"function will be called as func().")]
		public void OnAllKilled(Actor[] actors, LuaFunction func)
		{
			var group = actors.ToList();
			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnMemberKilled = m =>
			{
				try
				{
					group.Remove(m);
					if (!group.Any())
					{
						copy.Call();
						copy.Dispose();
					}
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			foreach (var a in group)
				GetScriptTriggers(a).OnKilledInternal += OnMemberKilled;
		}

		[Desc("Call a function when one of the actors in a group is killed. The callback " +
			"function will be called as func(Actor killed).")]
		public void OnAnyKilled(Actor[] actors, LuaFunction func)
		{
			var called = false;
			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnMemberKilled = m =>
			{
				try
				{
					if (called)
						return;

					using (var killed = m.ToLuaValue(context))
						copy.Call(killed).Dispose();

					copy.Dispose();
					called = true;
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			foreach (var a in actors)
				GetScriptTriggers(a).OnKilledInternal += OnMemberKilled;
		}

		[Desc("Call a function when this actor produces another actor. " +
			"The callback function will be called as func(Actor producer, Actor produced).")]
		public void OnProduction(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnProduction, func, context);
		}

		[Desc("Call a function when this player completes all primary objectives. " +
			"The callback function will be called as func(Player player).")]
		public void OnPlayerWon(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnPlayerWon, func, context);
		}

		[Desc("Call a function when this player fails any primary objective. " +
			"The callback function will be called as func(Player player).")]
		public void OnPlayerLost(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnPlayerLost, func, context);
		}

		[Desc("Call a function when this player is assigned a new objective. " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveAdded(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveAdded, func, context);
		}

		[Desc("Call a function when this player completes an objective. " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveCompleted(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveCompleted, func, context);
		}

		[Desc("Call a function when this player fails an objective. " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveFailed(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveFailed, func, context);
		}

		[Desc("Call a function when this actor is added to the world. " +
			"The callback function will be called as func(Actor self).")]
		public void OnAddedToWorld(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnAddedToWorld, func, context);
		}

		[Desc("Call a function when this actor is removed from the world. " +
			"The callback function will be called as func(Actor self).")]
		public void OnRemovedFromWorld(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnRemovedFromWorld, func, context);
		}

		[Desc("Call a function when all of the actors in a group have been removed from the world. " +
			"The callback function will be called as func().")]
		public void OnAllRemovedFromWorld(Actor[] actors, LuaFunction func)
		{
			var group = actors.ToList();

			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnMemberRemoved = m =>
			{
				try
				{
					group.Remove(m);
					if (!group.Any())
					{
						copy.Call().Dispose();
						copy.Dispose();
					}
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			foreach (var a in group)
				GetScriptTriggers(a).OnRemovedInternal += OnMemberRemoved;
		}

		[Desc("Call a function when this actor is captured. The callback function " +
			"will be called as func(Actor self, Actor captor, Player oldOwner, Player newOwner).")]
		public void OnCapture(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnCapture, func, context);
		}

		[Desc("Call a function when this actor is killed or captured. " +
			"The callback function will be called as func().")]
		public void OnKilledOrCaptured(Actor a, LuaFunction func)
		{
			var called = false;

			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnKilledOrCaptured = m =>
			{
				try
				{
					if (called)
						return;

					copy.Call().Dispose();
					copy.Dispose();
					called = true;
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			GetScriptTriggers(a).OnCapturedInternal += OnKilledOrCaptured;
			GetScriptTriggers(a).OnKilledInternal += OnKilledOrCaptured;
		}

		[Desc("Call a function when all of the actors in a group have been killed or captured. " +
			"The callback function will be called as func().")]
		public void OnAllKilledOrCaptured(Actor[] actors, LuaFunction func)
		{
			var group = actors.ToList();

			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnMemberKilledOrCaptured = m =>
			{
				try
				{
					if (!group.Contains(m))
						return;

					group.Remove(m);
					if (!group.Any())
					{
						copy.Call().Dispose();
						copy.Dispose();
					}
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			foreach (var a in group)
			{
				GetScriptTriggers(a).OnCapturedInternal += OnMemberKilledOrCaptured;
				GetScriptTriggers(a).OnKilledInternal += OnMemberKilledOrCaptured;
			}
		}

		[Desc("Call a function when a ground-based actor enters this cell footprint." +
			"Returns the trigger id for later removal using RemoveFootprintTrigger(int id)." +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnEnteredFootprint(CPos[] cells, LuaFunction func)
		{
			var triggerId = 0;
			var onEntry = (LuaFunction)func.CopyReference();
			Action<Actor> invokeEntry = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(context))
					using (var id = triggerId.ToLuaValue(context))
						onEntry.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			triggerId = context.World.ActorMap.AddCellTrigger(cells, invokeEntry, null);

			return triggerId;
		}

		[Desc("Call a function when a ground-based actor leaves this cell footprint." +
			"Returns the trigger id for later removal using RemoveFootprintTrigger(int id)." +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnExitedFootprint(CPos[] cells, LuaFunction func)
		{
			var triggerId = 0;
			var onExit = (LuaFunction)func.CopyReference();
			Action<Actor> invokeExit = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(context))
					using (var id = triggerId.ToLuaValue(context))
						onExit.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			triggerId = context.World.ActorMap.AddCellTrigger(cells, null, invokeExit);

			return triggerId;
		}

		[Desc("Removes a previously created footprint trigger.")]
		public void RemoveFootprintTrigger(int id)
		{
			context.World.ActorMap.RemoveCellTrigger(id);
		}

		[Desc("Call a function when an actor enters this range." +
			"Returns the trigger id for later removal using RemoveProximityTrigger(int id)." +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnEnteredProximityTrigger(WPos pos, WRange range, LuaFunction func)
		{
			var triggerId = 0;
			var onEntry = (LuaFunction)func.CopyReference();
			Action<Actor> invokeEntry = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(context))
					using (var id = triggerId.ToLuaValue(context))
						onEntry.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			triggerId = context.World.ActorMap.AddProximityTrigger(pos, range, invokeEntry, null);

			return triggerId;
		}

		[Desc("Call a function when an actor leaves this range." +
			"Returns the trigger id for later removal using RemoveProximityTrigger(int id)." +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnExitedProximityTrigger(WPos pos, WRange range, LuaFunction func)
		{
			var triggerId = 0;
			var onExit = (LuaFunction)func.CopyReference();
			Action<Actor> invokeExit = a =>
			{
				try
				{
					using (var luaActor = a.ToLuaValue(context))
					using (var id = triggerId.ToLuaValue(context))
						onExit.Call(luaActor, id).Dispose();
				}
				catch (Exception e)
				{
					context.FatalError(e.Message);
				}
			};

			triggerId = context.World.ActorMap.AddProximityTrigger(pos, range, null, invokeExit);

			return triggerId;
		}

		[Desc("Removes a previously created proximitry trigger.")]
		public void RemoveProximityTrigger(int id)
		{
			context.World.ActorMap.RemoveProximityTrigger(id);
		}

		[Desc("Call a function when this actor is infiltrated. The callback function " +
			"will be called as func(Actor self, Actor infiltrator).")]
		public void OnInfiltrated(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnInfiltrated, func, context);
		}

		[Desc("Removes all triggers from this actor." +
			"Note that the removal will only take effect at the end of a tick, " +
			"so you must not add new triggers at the same time that you are calling this function.")]
		public void ClearAll(Actor a)
		{
			GetScriptTriggers(a).ClearAll();
		}

		[Desc("Removes the specified trigger from this actor."  +
			"Note that the removal will only take effect at the end of a tick, " +
			"so you must not add new triggers at the same time that you are calling this function.")]
		public void Clear(Actor a, string triggerName)
		{
			var trigger = (Trigger)Enum.Parse(typeof(Trigger), triggerName);

			GetScriptTriggers(a).Clear(trigger);
		}
	}
}
