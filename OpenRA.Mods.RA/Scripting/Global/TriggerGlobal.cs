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

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Trigger")]
	public class TriggerGlobal : ScriptGlobal
	{
		public TriggerGlobal(ScriptContext context) : base(context) { }

		static ScriptTriggers GetScriptTriggers(Actor a)
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
				catch (LuaException e)
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
		public void OnAllKilled(LuaTable actors, LuaFunction func)
		{
			var group = new List<Actor>();
			foreach (var kv in actors)
			{
				Actor actor;
				if (!kv.Value.TryGetClrValue<Actor>(out actor))
					throw new LuaException("OnAllKilled requires a table of int,Actor pairs. Recieved {0},{1}".F(kv.Key.GetType().Name, kv.Value.GetType().Name));

				group.Add(actor);
			}

			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnMemberKilled = m =>
			{
				group.Remove(m);
				if (!group.Any())
				{
					copy.Call();
					copy.Dispose();
				}
			};

			foreach (var a in group)
				GetScriptTriggers(a).OnKilledInternal += OnMemberKilled;
		}

		[Desc("Call a function when one of the actors in a group is killed. The callback " +
			"function will be called as func(Actor killed).")]
		public void OnAnyKilled(LuaTable actors, LuaFunction func)
		{
			var group = new List<Actor>();
			foreach (var kv in actors)
			{
				Actor actor;
				if (!kv.Value.TryGetClrValue<Actor>(out actor))
					throw new LuaException("OnAnyKilled requires a table of int,Actor pairs. Recieved {0},{1}".F(kv.Key.GetType().Name, kv.Value.GetType().Name));

				group.Add(actor);
			}

			var called = false;
			var copy = (LuaFunction)func.CopyReference();
			Action<Actor> OnMemberKilled = m =>
			{
				if (called)
					return;

				using (var killed = m.ToLuaValue(context))
					copy.Call(killed).Dispose();

				copy.Dispose();
				called = true;
			};

			foreach (var a in group)
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

		[Desc("Call a function when this actor is captured. The callback function " +
		      "will be called as func(Actor self, Actor captor, Player oldOwner, Player newOwner).")]
		public void OnCapture(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnCapture, func, context);
		}

		static CPos[] MakeCellFootprint(LuaTable table)
		{
			var cells = new List<CPos>();
			foreach (var kv in table)
			{
				CPos cell;
				if (!kv.Value.TryGetClrValue<CPos>(out cell))
					throw new LuaException("Cell footprints must be specified as a table of int,Cell pairs. Recieved {0},{1}".F(kv.Key.GetType().Name, kv.Value.GetType().Name));

				cells.Add(cell);
			}

			return cells.ToArray();
		}

		[Desc("Call a function when a ground-based actor enters this cell footprint." +
			"Returns the trigger id for later removal using RemoveFootprintTrigger(int id)." +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnEnteredFootprint(LuaTable cells, LuaFunction func)
		{
			var triggerId = 0;
			var onEntry = (LuaFunction)func.CopyReference();
			Action<Actor> invokeEntry = a =>
			{
				using (var luaActor = a.ToLuaValue(context))
				using (var id = triggerId.ToLuaValue(context))
					onEntry.Call(luaActor, id).Dispose();
			};

			triggerId = context.World.ActorMap.AddCellTrigger(MakeCellFootprint(cells), invokeEntry, null);

			return triggerId;
		}

		[Desc("Call a function when a ground-based actor leaves this cell footprint." +
			"Returns the trigger id for later removal using RemoveFootprintTrigger(int id)." +
			"The callback function will be called as func(Actor a, int id).")]
		public int OnExitedFootprint(LuaTable cells, LuaFunction func)
		{
			var triggerId = 0;
			var onExit = (LuaFunction)func.CopyReference();
			Action<Actor> invokeExit = a =>
			{
				using (var luaActor = a.ToLuaValue(context))
				using (var id = triggerId.ToLuaValue(context))
					onExit.Call(luaActor, id).Dispose();
			};

			triggerId = context.World.ActorMap.AddCellTrigger(MakeCellFootprint(cells), null, invokeExit);

			return triggerId;
		}

		[Desc("Removes a previously created footprint trigger.")]
		public void RemoveFootprintTrigger(int id)
		{
			context.World.ActorMap.RemoveCellTrigger(id);
		}

		[Desc("Removes all triggers from this actor.")]
		public void ClearAll(Actor a)
		{
			GetScriptTriggers(a).ClearAll();
		}

		[Desc("Removes the specified trigger from this actor.")]
		public void Clear(Actor a, string triggerName)
		{
			var trigger = (Trigger)Enum.Parse(typeof(Trigger), triggerName);

			GetScriptTriggers(a).Clear(trigger);
		}
	}
}
