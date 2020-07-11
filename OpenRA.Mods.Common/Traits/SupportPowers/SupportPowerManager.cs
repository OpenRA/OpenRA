#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class SupportPowerManagerInfo : TraitInfo, Requires<DeveloperModeInfo>, Requires<TechTreeInfo>
	{
		public override object Create(ActorInitializer init) { return new SupportPowerManager(init); }
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
					Powers.Add(key, t.CreateInstance(key, this));

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

		static readonly SupportPowerInstance[] NoInstances = { };

		public IEnumerable<SupportPowerInstance> GetPowersForActor(Actor a)
		{
			if (Powers.Count == 0 || a.Owner != Self.Owner || !a.Info.HasTraitInfo<SupportPowerInfo>())
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
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}

	public class SupportPowerInstance
	{
		protected readonly SupportPowerManager Manager;

		public readonly string Key;

		public readonly List<SupportPower> Instances = new List<SupportPower>();
		public readonly int TotalTicks;

		protected int remainingSubTicks;
		public int RemainingTicks { get { return remainingSubTicks / 100; } }
		public bool Active { get; private set; }
		public bool Disabled
		{
			get
			{
				return Manager.Self.Owner.WinState == WinState.Lost ||
					(!prereqsAvailable && !Manager.DevMode.AllTech) ||
					!instancesEnabled ||
					oneShotFired;
			}
		}

		public SupportPowerInfo Info { get { return Instances.Select(i => i.Info).FirstOrDefault(); } }
		public bool Ready { get { return Active && RemainingTicks == 0; } }

		bool instancesEnabled;
		bool prereqsAvailable = true;
		bool oneShotFired;
		protected bool notifiedCharging;
		bool notifiedReady;

		public void ResetTimer()
		{
			remainingSubTicks = TotalTicks * 100;
		}

		public SupportPowerInstance(string key, SupportPowerInfo info, SupportPowerManager manager)
		{
			Key = key;
			TotalTicks = info.ChargeInterval;
			remainingSubTicks = info.StartFullyCharged ? 0 : TotalTicks * 100;

			Manager = manager;
		}

		public virtual void PrerequisitesAvailable(bool available)
		{
			prereqsAvailable = available;

			if (!available)
				remainingSubTicks = TotalTicks * 100;
		}

		public virtual void Tick()
		{
			instancesEnabled = Instances.Any(i => !i.IsTraitDisabled);
			if (!instancesEnabled)
				remainingSubTicks = TotalTicks * 100;

			Active = !Disabled && Instances.Any(i => !i.IsTraitPaused);
			if (!Active)
				return;

			if (Active)
			{
				var power = Instances.First();
				if (Manager.DevMode.FastCharge && remainingSubTicks > 2500)
					remainingSubTicks = 2500;

				if (remainingSubTicks > 0)
					remainingSubTicks = (remainingSubTicks - 100).Clamp(0, TotalTicks * 100);

				if (!notifiedCharging)
				{
					power.Charging(power.Self, Key);
					notifiedCharging = true;
				}

				if (RemainingTicks == 0
					&& !notifiedReady)
				{
					power.Charged(power.Self, Key);
					notifiedReady = true;
				}
			}
		}

		public virtual void Target()
		{
			if (!Ready)
				return;

			var power = Instances.FirstOrDefault(i => !i.IsTraitPaused);
			if (power == null)
				return;

			power.SelectTarget(power.Self, Key, Manager);
		}

		public virtual void Activate(Order order)
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
			power.Activate(power.Self, order, Manager);
			remainingSubTicks = TotalTicks * 100;
			notifiedCharging = notifiedReady = false;

			if (Info.OneShot)
			{
				PrerequisitesAvailable(false);
				oneShotFired = true;
			}
		}

		public virtual string IconOverlayTextOverride()
		{
			return null;
		}

		public virtual string TooltipTimeTextOverride()
		{
			return null;
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
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }
		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? cursor : "generic-blocked";
		}
	}
}
