#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Scripting
{
	public enum Trigger
	{
		OnIdle, OnDamaged, OnKilled, OnProduction, OnOtherProduction, OnPlayerWon, OnPlayerLost,
		OnObjectiveAdded, OnObjectiveCompleted, OnObjectiveFailed, OnCapture, OnInfiltrated,
		OnAddedToWorld, OnRemovedFromWorld, OnDiscovered, OnPlayerDiscovered
	}

	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new ScriptTriggers(init.World, init.Self); }
	}

	public sealed class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, INotifyOtherProduction,
		INotifyObjectivesUpdated, INotifyCapture, INotifyInfiltrated, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyDiscovered, INotifyActorDisposing
	{
		readonly World world;
		readonly Actor self;

		public event Action<Actor> OnKilledInternal = _ => { };
		public event Action<Actor> OnCapturedInternal = _ => { };
		public event Action<Actor> OnRemovedInternal = _ => { };
		public event Action<Actor, Actor> OnProducedInternal = (a, b) => { };
		public event Action<Actor, Actor> OnOtherProducedInternal = (a, b) => { };

		readonly Dictionary<Trigger, List<Triggerable>> triggers = new Dictionary<Trigger, List<Triggerable>>();

		struct Triggerable : IDisposable
		{
			public readonly LuaFunction Function;
			public readonly ScriptContext Context;
			public readonly LuaValue Self;
			public Triggerable(LuaFunction function, ScriptContext context, Actor self)
			{
				Function = (LuaFunction)function.CopyReference();
				Context = context;
				Self = self.ToLuaValue(Context);
			}

			public void Dispose()
			{
				Function.Dispose();
				Self.Dispose();
			}
		}

		public ScriptTriggers(World world, Actor self)
		{
			this.world = world;
			this.self = self;

			foreach (Trigger t in Enum.GetValues(typeof(Trigger)))
				triggers.Add(t, new List<Triggerable>());
		}

		public void RegisterCallback(Trigger trigger, LuaFunction func, ScriptContext context)
		{
			triggers[trigger].Add(new Triggerable(func, context, self));
		}

		public bool HasAnyCallbacksFor(Trigger trigger)
		{
			return triggers[trigger].Count > 0;
		}

		public void TickIdle(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnIdle])
			{
				try
				{
					f.Function.Call(f.Self).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnDamaged])
			{
				try
				{
					using (var b = e.Attacker.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in triggers[Trigger.OnKilled])
			{
				try
				{
					using (var b = e.Attacker.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnKilledInternal(self);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in triggers[Trigger.OnProduction])
			{
				try
				{
					using (var b = other.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnProducedInternal(self, other);
		}

		public void OnPlayerWon(Player player)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnPlayerWon])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Context))
						f.Function.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnPlayerLost(Player player)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnPlayerLost])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Context))
						f.Function.Call(a).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveAdded(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnObjectiveAdded])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Context))
					using (var b = id.ToLuaValue(f.Context))
						f.Function.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveCompleted(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnObjectiveCompleted])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Context))
					using (var b = id.ToLuaValue(f.Context))
						f.Function.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveFailed(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnObjectiveFailed])
			{
				try
				{
					using (var a = player.ToLuaValue(f.Context))
					using (var b = id.ToLuaValue(f.Context))
						f.Function.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnCapture])
			{
				try
				{
					using (var b = captor.ToLuaValue(f.Context))
					using (var c = oldOwner.ToLuaValue(f.Context))
					using (var d = newOwner.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b, c, d).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnCapturedInternal(self);
		}

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnInfiltrated])
			{
				try
				{
					using (var b = infiltrator.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void AddedToWorld(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnAddedToWorld])
			{
				try
				{
					f.Function.Call(f.Self).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void RemovedFromWorld(Actor self)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in triggers[Trigger.OnRemovedFromWorld])
			{
				try
				{
					f.Function.Call(f.Self).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnRemovedInternal(self);
		}

		public void UnitProducedByOther(Actor self, Actor producee, Actor produced)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in triggers[Trigger.OnOtherProduction])
			{
				try
				{
					using (var a = producee.ToLuaValue(f.Context))
					using (var b = produced.ToLuaValue(f.Context))
						f.Function.Call(a, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnOtherProducedInternal(producee, produced);
		}

		public void OnDiscovered(Actor self, Player discoverer, bool playNotification)
		{
			if (world.Disposing)
				return;

			foreach (var f in triggers[Trigger.OnDiscovered])
			{
				try
				{
					using (var b = discoverer.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}

			foreach (var f in triggers[Trigger.OnPlayerDiscovered])
			{
				try
				{
					using (var a = self.Owner.ToLuaValue(f.Context))
					using (var b = discoverer.ToLuaValue(f.Context))
						f.Function.Call(a, b, f.Self).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Clear(Trigger trigger)
		{
			world.AddFrameEndTask(w =>
			{
				var triggerables = triggers[trigger];
				foreach (var f in triggerables)
					f.Dispose();
				triggerables.Clear();
			});
		}

		public void ClearAll()
		{
			foreach (Trigger t in Enum.GetValues(typeof(Trigger)))
				Clear(t);
		}

		public void Disposing(Actor self)
		{
			ClearAll();
		}
	}
}
