#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SupportPowerManagerInfo : ITraitInfo, Requires<DeveloperModeInfo>
	{
		public object Create(ActorInitializer init) { return new SupportPowerManager(init); }
	}

	public class SupportPowerManager : ITick, IResolveOrder
	{
		public readonly Actor self;
		public readonly Dictionary<string, SupportPowerInstance> Powers = new Dictionary<string, SupportPowerInstance>();

		public readonly DeveloperMode DevMode;
		public readonly Lazy<RadarPings> RadarPings;

		public SupportPowerManager(ActorInitializer init)
		{
			self = init.self;
			DevMode = init.self.Trait<DeveloperMode>();
			RadarPings = Exts.Lazy(() => init.world.WorldActor.TraitOrDefault<RadarPings>());

			init.world.ActorAdded += ActorAdded;
			init.world.ActorRemoved += ActorRemoved;
		}

		static string MakeKey(SupportPower sp)
		{
			return sp.Info.AllowMultiple ? sp.Info.OrderName + "_" + sp.self.ActorID : sp.Info.OrderName;
		}

		void ActorAdded(Actor a)
		{
			if (a.Owner != self.Owner || !a.HasTrait<SupportPower>())
				return;

			foreach (var t in a.TraitsImplementing<SupportPower>())
			{
				var key = MakeKey(t);

				if (Powers.ContainsKey(key))
				{
					Powers[key].Instances.Add(t);
				}
				else
				{
					var si = new SupportPowerInstance(key, this)
					{
						Instances = new List<SupportPower>() { t },
						RemainingTime = t.Info.ChargeTime * 25,
						TotalTime = t.Info.ChargeTime * 25,
					};

					Powers.Add(key, si);
				}
			}
		}

		void ActorRemoved(Actor a)
		{
			if (a.Owner != self.Owner || !a.HasTrait<SupportPower>())
				return;

			foreach (var t in a.TraitsImplementing<SupportPower>())
			{
				var key = MakeKey(t);
				Powers[key].Instances.Remove(t);
				if (Powers[key].Instances.Count == 0 && !Powers[key].Disabled)
					Powers.Remove(key);
			}
		}

		public void Tick(Actor self)
		{
			foreach (var power in Powers.Values)
				power.Tick();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			// order.OrderString is the key of the support power
			if (Powers.ContainsKey(order.OrderString))
				Powers[order.OrderString].Activate(order);
		}

		public void Target(string key)
		{
			if (Powers.ContainsKey(key))
				Powers[key].Target();
		}

		static readonly SupportPowerInstance[] NoInstances = { };

		public IEnumerable<SupportPowerInstance> GetPowersForActor(Actor a)
		{
			if (a.Owner != self.Owner || !a.HasTrait<SupportPower>())
				return NoInstances;

			return a.TraitsImplementing<SupportPower>()
				.Select(t => Powers[MakeKey(t)]);
		}
	}

	public class SupportPowerInstance
	{
		readonly SupportPowerManager Manager;
		readonly string Key;

		public List<SupportPower> Instances;
		public int RemainingTime;
		public int TotalTime;
		public bool Active { get; private set; }
		public bool Disabled { get; private set; }

		public SupportPowerInfo Info { get { return Instances.Select(i => i.Info).FirstOrDefault(); } }
		public bool Ready { get { return Active && RemainingTime == 0; } }

		public SupportPowerInstance(string key, SupportPowerManager manager)
		{
			Manager = manager;
			Key = key;
		}

		static bool InstanceDisabled(SupportPower sp)
		{
			return sp.self.TraitsImplementing<IDisable>().Any(d => d.Disabled);
		}

		bool notifiedCharging;
		bool notifiedReady;
		public void Tick()
		{
			Active = !Disabled && Instances.Any(i => !i.self.IsDisabled());
			if (!Active)
				return;

			if (Active)
			{
				var power = Instances.First();
				if (Manager.DevMode.FastCharge && RemainingTime > 25)
					RemainingTime = 25;

				if (RemainingTime > 0) --RemainingTime;
				if (!notifiedCharging)
				{
					power.Charging(power.self, Key);
					notifiedCharging = true;
				}

				if (RemainingTime == 0
					&& !notifiedReady)
				{
					power.Charged(power.self, Key);
					notifiedReady = true;
				}
			}
		}

		public void Target()
		{
			if (!Ready)
				return;

			Manager.self.World.OrderGenerator = Instances.First().OrderGenerator(Key, Manager);
		}

		public void Activate(Order order)
		{
			if (!Ready)
				return;

			var power = Instances.First(i => !InstanceDisabled(i));

			// Note: order.Subject is the *player* actor
			power.Activate(power.self, order, Manager);
			RemainingTime = TotalTime;
			notifiedCharging = notifiedReady = false;

			if (Info.OneShot)
				Disabled = true;
		}
	}

	public class SelectGenericPowerTarget : IOrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly string order;
		readonly string cursor;
		readonly MouseButton expectedButton;

		public SelectGenericPowerTarget(string order, SupportPowerManager manager, string cursor, MouseButton button)
		{
			this.manager = manager;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == expectedButton && world.Map.IsInMap(xy))
				yield return new Order(order, manager.self, false) { TargetLocation = xy };
		}

		public virtual void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld(WorldRenderer wr, World world) { }
		public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.IsInMap(xy) ? cursor : "generic-blocked"; }
	}
}
