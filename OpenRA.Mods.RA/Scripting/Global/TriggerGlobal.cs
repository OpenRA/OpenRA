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

		[Desc("Call a function when this player completes an objective " +
			"The callback function will be called as func(Player player, int objectiveID).")]
		public void OnObjectiveCompleted(Player player, LuaFunction func)
		{
			GetScriptTriggers(player.PlayerActor).RegisterCallback(Trigger.OnObjectiveCompleted, func, context);
		}

		[Desc("Call a function when this player fails an objective " +
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

		[Desc("Call a function when an actor enters a region. The callback " +
			"function will be called as func(Player owner, Actor self, string region).")]
		public void OnEnterRegion(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnRegionEntered, func, context);
		}

		[Desc("Call a function when an actor leaves a region. The callback " +
			"function will be called as func(Player owner, Actor self, string region).")]
		public void OnLeaveRegion(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterCallback(Trigger.OnRegionLeft, func, context);
		}

		[Desc("Removes all triggers from this actor")]
		public void ClearAll(Actor a)
		{
			GetScriptTriggers(a).ClearAll();
		}

		[Desc("Removes the specified trigger from this actor")]
		public void Clear(Actor a, string triggerName)
		{
			var trigger = (Trigger)Enum.Parse(typeof(Trigger), triggerName);

			GetScriptTriggers(a).Clear(trigger);
		}
	}
}
