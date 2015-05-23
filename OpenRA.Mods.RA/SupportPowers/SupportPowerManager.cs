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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Attach this to the player actor.")]
	public class SupportPowerManagerInfo : ITraitInfo, Requires<DeveloperModeInfo>, Requires<TechTreeInfo>
	{
		public object Create(ActorInitializer init) { return new SupportPowerManager(init); }
	}

	public class SupportPowerManager : ITick, IResolveOrder, ITechTreeElement
	{
		public readonly Actor Self;
		public readonly Dictionary<string, SupportPowerInstance> Powers = new Dictionary<string, SupportPowerInstance>();

		public readonly DeveloperMode DevMode;
		public readonly TechTree TechTree;
		public readonly Lazy<RadarPings> RadarPings;

		public SupportPowerManager(ActorInitializer init)
		{
			Self = init.Self;
			DevMode = Self.Trait<DeveloperMode>();
			TechTree = Self.Trait<TechTree>();
			RadarPings = Exts.Lazy(() => init.World.WorldActor.TraitOrDefault<RadarPings>());

			init.World.ActorAdded += ActorAdded;
			init.World.ActorRemoved += ActorRemoved;
		}

		static string MakeKey(SupportPower sp)
		{
			return sp.Info.AllowMultiple ? sp.Info.OrderName + "_" + sp.Self.ActorID : sp.Info.OrderName;
		}

		void ActorAdded(Actor a)
		{
			if (a.Owner != Self.Owner)
				return;

			foreach (var t in a.TraitsImplementing<SupportPower>())
			{
				var key = MakeKey(t);

				if (!Powers.ContainsKey(key))
				{
					Powers.Add(key, new SupportPowerInstance(key, this)
					{
						Instances = new List<SupportPower>(),
						RemainingTime = t.Info.ChargeTime * 25,
						TotalTime = t.Info.ChargeTime * 25,
					});

					if (t.Info.Prerequisites.Any())
					{
						TechTree.Add(key, t.Info.Prerequisites, 0, this);
						TechTree.Update();
					}
				}

				Powers[key].Instances.Add(t);
			}
		}

		void ActorRemoved(Actor a)
		{
			if (a.Owner != Self.Owner || !a.HasTrait<SupportPower>())
				return;

			foreach (var t in a.TraitsImplementing<SupportPower>())
			{
				var key = MakeKey(t);
				Powers[key].Instances.Remove(t);

				if (Powers[key].Instances.Count == 0 && !Powers[key].Disabled)
				{
					Powers.Remove(key);
					TechTree.Remove(key);
					TechTree.Update();
				}
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

		// Deprecated. Remove after SupportPowerBinWidget is removed.
		public void Target(string key)
		{
			if (Powers.ContainsKey(key))
				Powers[key].Target();
		}

		static readonly SupportPowerInstance[] NoInstances = { };

		public IEnumerable<SupportPowerInstance> GetPowersForActor(Actor a)
		{
			if (a.Owner != Self.Owner || !a.HasTrait<SupportPower>())
				return NoInstances;

			return a.TraitsImplementing<SupportPower>()
				.Select(t => Powers[MakeKey(t)]);
		}

		public void PrerequisitesAvailable(string key)
		{
			SupportPowerInstance sp;
			if (!Powers.TryGetValue(key, out sp))
				return;

			sp.Disabled = false;
		}

		public void PrerequisitesUnavailable(string key)
		{
			SupportPowerInstance sp;
			if (!Powers.TryGetValue(key, out sp))
				return;

			sp.Disabled = true;
			sp.RemainingTime = sp.TotalTime;
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}

	public class SupportPowerInstance
	{
		readonly SupportPowerManager manager;
		readonly string key;

		public List<SupportPower> Instances;
		public int RemainingTime;
		public int TotalTime;
		public bool Active { get; private set; }
		public bool Disabled { get; set; }

		public SupportPowerInfo Info { get { return Instances.Select(i => i.Info).FirstOrDefault(); } }
		public bool Ready { get { return Active && RemainingTime == 0; } }

		public SupportPowerInstance(string key, SupportPowerManager manager)
		{
			this.manager = manager;
			this.key = key;
		}

		static bool InstanceDisabled(SupportPower sp)
		{
			return sp.Self.TraitsImplementing<IDisable>().Any(d => d.Disabled);
		}

		bool notifiedCharging;
		bool notifiedReady;
		public void Tick()
		{
			Active = !Disabled && Instances.Any(i => !i.Self.IsDisabled());
			if (!Active)
				return;

			if (Active)
			{
				var power = Instances.First();
				if (manager.DevMode.FastCharge && RemainingTime > 25)
					RemainingTime = 25;

				if (RemainingTime > 0)
					--RemainingTime;
				if (!notifiedCharging)
				{
					power.Charging(power.Self, key);
					notifiedCharging = true;
				}

				if (RemainingTime == 0
					&& !notifiedReady)
				{
					power.Charged(power.Self, key);
					notifiedReady = true;
				}
			}
		}

		public void Target()
		{
			if (!Ready)
				return;

			manager.Self.World.OrderGenerator = Instances.First().OrderGenerator(key, manager);
		}

		public void Activate(Order order)
		{
			if (!Ready)
				return;

			var power = Instances.First(i => !InstanceDisabled(i));

			// Note: order.Subject is the *player* actor
			power.Activate(power.Self, order, manager);
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
			if (mi.Button == expectedButton && world.Map.Contains(xy))
				yield return new Order(order, manager.Self, false) { TargetLocation = xy, SuppressVisualFeedback = true };
		}

		public virtual void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world) { yield break; }
		public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.Contains(xy) ? cursor : "generic-blocked"; }
	}
}
