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
using System.Collections.Generic;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	public enum Trigger
	{
		OnIdle, OnDamaged, OnKilled, OnProduction, OnOtherProduction, OnPlayerWon, OnPlayerLost,
		OnObjectiveAdded, OnObjectiveCompleted, OnObjectiveFailed, OnCapture, OnInfiltrated,
		OnAddedToWorld, OnRemovedFromWorld, OnDiscovered, OnPlayerDiscovered,
		OnPassengerEntered, OnPassengerExited, OnSold, OnTimerExpired
	}

	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new ScriptTriggers(init.World, init.Self); }
	}

	public sealed class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, INotifyOtherProduction,
		INotifyObjectivesUpdated, INotifyCapture, INotifyInfiltrated, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyDiscovered, INotifyActorDisposing,
		INotifyPassengerEntered, INotifyPassengerExited, INotifySold, INotifyWinStateChanged, INotifyTimeLimit
	{
		readonly World world;
		readonly Actor self;

		public event Action<Actor> OnKilledInternal = _ => { };
		public event Action<Actor> OnCapturedInternal = _ => { };
		public event Action<Actor> OnRemovedInternal = _ => { };
		public event Action<Actor> OnAddedInternal = _ => { };
		public event Action<Actor, Actor> OnProducedInternal = (a, b) => { };
		public event Action<Actor, Actor> OnOtherProducedInternal = (a, b) => { };

		readonly List<Triggerable>[] triggerables = Exts.MakeArray(Enum.GetValues(typeof(Trigger)).Length, _ => new List<Triggerable>());

		readonly struct Triggerable : IDisposable
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
		}

		List<Triggerable> Triggerables(Trigger trigger)
		{
			return triggerables[(int)trigger];
		}

		public void RegisterCallback(Trigger trigger, LuaFunction func, ScriptContext context)
		{
			Triggerables(trigger).Add(new Triggerable(func, context, self));
		}

		public bool HasAnyCallbacksFor(Trigger trigger)
		{
			return Triggerables(trigger).Count > 0;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnIdle))
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

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnDamaged))
			{
				try
				{
					using (var b = e.Attacker.ToLuaValue(f.Context))
					using (var c = e.Damage.Value.ToLuaValue(f.Context))
						f.Function.Call(f.Self, b, c).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggerables(Trigger.OnKilled))
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

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggerables(Trigger.OnProduction))
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

		void INotifyWinStateChanged.OnPlayerWon(Player player)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnPlayerWon))
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

		void INotifyWinStateChanged.OnPlayerLost(Player player)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnPlayerLost))
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

		void INotifyObjectivesUpdated.OnObjectiveAdded(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnObjectiveAdded))
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

		void INotifyObjectivesUpdated.OnObjectiveCompleted(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnObjectiveCompleted))
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

		void INotifyObjectivesUpdated.OnObjectiveFailed(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnObjectiveFailed))
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

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnCapture))
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

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnInfiltrated))
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

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnAddedToWorld))
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
			OnAddedInternal(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggerables(Trigger.OnRemovedFromWorld))
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

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggerables(Trigger.OnSold))
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

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producee, Actor produced, string productionType, TypeDictionary init)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggerables(Trigger.OnOtherProduction))
			{
				try
				{
					using (var a = producee.ToLuaValue(f.Context))
					using (var b = produced.ToLuaValue(f.Context))
					using (var c = productionType.ToLuaValue(f.Context))
						f.Function.Call(a, b, c).Dispose();
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

		void INotifyDiscovered.OnDiscovered(Actor self, Player discoverer, bool playNotification)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnDiscovered))
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

			foreach (var f in Triggerables(Trigger.OnPlayerDiscovered))
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

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnPassengerEntered))
			{
				try
				{
					using (var trans = self.ToLuaValue(f.Context))
					using (var pass = passenger.ToLuaValue(f.Context))
						f.Function.Call(trans, pass).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnPassengerExited))
			{
				try
				{
					using (var trans = self.ToLuaValue(f.Context))
					using (var pass = passenger.ToLuaValue(f.Context))
						f.Function.Call(trans, pass).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex.Message);
					return;
				}
			}
		}

		void INotifyTimeLimit.NotifyTimerExpired(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(Trigger.OnTimerExpired))
			{
				try
				{
					f.Function.Call().Dispose();
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
				var triggerables = Triggerables(trigger);
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

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ClearAll();
		}
	}
}
