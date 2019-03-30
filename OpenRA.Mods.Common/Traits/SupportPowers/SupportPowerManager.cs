#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
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
						RemainingTime = t.Info.StartFullyCharged ? 0 : t.Info.ChargeInterval,
						TotalTime = t.Info.ChargeInterval,
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
			if (a.Owner != Self.Owner || !a.Info.HasTraitInfo<SupportPowerInfo>())
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

		void ITick.Tick(Actor self)
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
			if (a.Owner != Self.Owner || !a.Info.HasTraitInfo<SupportPowerInfo>())
				return NoInstances;

			return a.TraitsImplementing<SupportPower>()
				.Select(t => Powers[MakeKey(t)])
				.Where(p => p.Instances.Any(i => !i.IsTraitDisabled && i.Self == a));
		}

		public void PrerequisitesAvailable(string key)
		{
			SupportPowerInstance sp;
			if (!Powers.TryGetValue(key, out sp))
				return;

			sp.PrerequisitesAvailable(true);
		}

		public void PrerequisitesUnavailable(string key)
		{
			SupportPowerInstance sp;
			if (!Powers.TryGetValue(key, out sp))
				return;

			sp.PrerequisitesAvailable(false);
			sp.RemainingTime = sp.TotalTime;
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}

	public class SupportPowerInstance
	{
		readonly SupportPowerManager manager;

		public readonly string Key;

		public List<SupportPower> Instances;
		public int RemainingTime;
		public int TotalTime;
		public bool Active { get; private set; }
		public bool Disabled { get { return (!prereqsAvailable && !manager.DevMode.AllTech) || !instancesEnabled || oneShotFired; } }

		public SupportPowerInfo Info { get { return Instances.Select(i => i.Info).FirstOrDefault(); } }
		public bool Ready { get { return Active && RemainingTime == 0; } }

		bool instancesEnabled;
		bool prereqsAvailable = true;
		bool oneShotFired;

		public SupportPowerInstance(string key, SupportPowerManager manager)
		{
			this.manager = manager;
			Key = key;
		}

		public void PrerequisitesAvailable(bool available)
		{
			prereqsAvailable = available;
		}

		bool notifiedCharging;
		bool notifiedReady;
		public void Tick()
		{
			instancesEnabled = Instances.Any(i => !i.IsTraitDisabled);
			if (!instancesEnabled)
				RemainingTime = TotalTime;

			Active = !Disabled && Instances.Any(i => !i.IsTraitPaused);
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
					power.Charging(power.Self, Key);
					notifiedCharging = true;
				}

				if (RemainingTime == 0
					&& !notifiedReady)
				{
					power.Charged(power.Self, Key);
					notifiedReady = true;
				}
			}
		}

		public void Target()
		{
			if (!Ready)
				return;

			var power = Instances.FirstOrDefault(i => !i.IsTraitPaused);
			if (power == null)
				return;

			power.SelectTarget(power.Self, Key, manager);
		}

		public void Activate(Order order)
		{
			if (!Ready)
				return;

			var power = Instances.Where(i => !i.IsTraitPaused && !i.IsTraitDisabled)
				.MinByOrDefault(a =>
				{
					if (a.Self.OccupiesSpace == null || order.Target.Type == TargetType.Invalid)
						return 0;

					return (a.Self.CenterPosition - order.Target.CenterPosition).HorizontalLengthSquared;
				});

			if (power == null)
				return;

			// Note: order.Subject is the *player* actor
			power.Activate(power.Self, order, manager);
			RemainingTime = TotalTime;
			notifiedCharging = notifiedReady = false;

			if (Info.OneShot)
			{
				PrerequisitesAvailable(false);
				oneShotFired = true;
			}
		}
	}

	public class SelectGenericPowerTarget : OrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly string order;
		readonly string cursor;
		readonly MouseButton expectedButton;

		public string OrderKey { get { return order; } }

		public SelectGenericPowerTarget(string order, SupportPowerManager manager, string cursor, MouseButton button)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.manager = manager;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == expectedButton && world.Map.Contains(cell))
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? cursor : "generic-blocked";
		}
	}
}
