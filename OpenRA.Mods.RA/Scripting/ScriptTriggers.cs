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
	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : TraitInfo<ScriptTriggers> { }

	public class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, IDisposable
	{
		public event Action<Actor> OnKilledInternal = _ => {};

		List<Pair<LuaFunction, ScriptContext>> onIdle = new List<Pair<LuaFunction, ScriptContext>>();
		List<Pair<LuaFunction, ScriptContext>> onDamaged = new List<Pair<LuaFunction, ScriptContext>>();
		List<Pair<LuaFunction, ScriptContext>> onKilled = new List<Pair<LuaFunction, ScriptContext>>();
		List<Pair<LuaFunction, ScriptContext>> onProduction = new List<Pair<LuaFunction, ScriptContext>>();

		public void RegisterIdleCallback(LuaFunction func, ScriptContext context)
		{
			onIdle.Add(Pair.New((LuaFunction)func.CopyReference(), context));
		}

		public void RegisterDamagedCallback(LuaFunction func, ScriptContext context)
		{
			onDamaged.Add(Pair.New((LuaFunction)func.CopyReference(), context));
		}

		public void RegisterKilledCallback(LuaFunction func, ScriptContext context)
		{
			onKilled.Add(Pair.New((LuaFunction)func.CopyReference(), context));
		}

		public void RegisterProductionCallback(LuaFunction func, ScriptContext context)
		{
			onProduction.Add(Pair.New((LuaFunction)func.CopyReference(), context));
		}

		public void TickIdle(Actor self)
		{
			foreach (var f in onIdle)
			{
				var a = self.ToLuaValue(f.Second);
				f.First.Call(a).Dispose();
				a.Dispose();
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			foreach (var f in onDamaged)
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
			foreach (var f in onKilled)
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
			foreach (var f in onProduction)
			{
				var a = self.ToLuaValue(f.Second);
				var b = other.ToLuaValue(f.Second);
				f.First.Call(a, b).Dispose();
				a.Dispose();
				b.Dispose();
			}
		}

		bool disposed;

		protected void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				var toDispose = new [] { onIdle, onDamaged, onKilled, onProduction };

				foreach (var f in toDispose.SelectMany(f => f))
					f.First.Dispose();

				foreach (var l in toDispose)
					l.Clear();
			}

			disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ScriptTriggers()
		{
			// Dispose unmanaged resources only
			Dispose(false);
		}
	}
}
