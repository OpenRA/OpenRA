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
			GetScriptTriggers(a).RegisterIdleCallback(func, context);
		}

		[Desc("Call a function when the actor is damaged. The callback " +
			"function will be called as func(Actor self, Actor attacker).")]
		public void OnDamaged(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterDamagedCallback(func, context);
		}

		[Desc("Call a function when the actor is killed. The callback " +
			"function will be called as func(Actor self, Actor killer).")]
		public void OnKilled(Actor a, LuaFunction func)
		{
			GetScriptTriggers(a).RegisterKilledCallback(func, context);
		}

		[Desc("Call a function when all of the actors in a group are killed. The callback " +
			"function will be called as func().")]
		public void OnAllKilled(LuaTable actors, LuaFunction func)
		{
			List<Actor> group = new List<Actor>();
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
			GetScriptTriggers(a).RegisterProductionCallback(func, context);
		}
	}
}
