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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public abstract class SupportPowerInfo : ITraitInfo
	{
		public readonly int ChargeTime = 0;
		public readonly string Icon = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly bool OneShot = false;
		public readonly string[] Prerequisites = {};

		public readonly string BeginChargeSound = null;
		public readonly string EndChargeSound = null;
		public readonly string SelectTargetSound = null;
		public readonly string InsufficientPowerSound = null;
		public readonly string LaunchSound = null;
		public readonly string IncomingSound = null;

		public readonly bool DisplayTimer = false;

		[Desc("Beacons are only supported on the Airstrike and Nuke powers")]
		public readonly bool DisplayBeacon = false;
		public readonly string BeaconPalettePrefix = "player";
		public readonly string BeaconPoster = null;
		public readonly string BeaconPosterPalette = "chrome";

		public readonly bool DisplayRadarPing = false;
		public readonly int RadarPingDuration = 5 * 25;

		public readonly string OrderName;
		public abstract object Create(ActorInitializer init);

		public SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class SupportPower : INotifyAddedToWorld, INotifyRemovedFromWorld, ITick, IResolveOrder, ITechTreeElement
	{

		public readonly Actor Self;
		public readonly SupportPowerInfo Info;
		readonly Lazy<RadarPings> RadarPings;
		readonly DeveloperMode devmode;
		readonly TechTree tt;
		protected RadarPing ping;
		public readonly string Key;
		public int TotalTime;
		public int RemainingTime;
		public bool HasPrerequisites { get; private set; }
		public bool Disabled { get { return !HasPrerequisites || Self.IsDisabled(); } }
		public bool Ready { get { return !Disabled && RemainingTime == 0; } }

		bool notifiedCharging;
		bool notifiedReady;

		public SupportPower(ActorInitializer init, SupportPowerInfo info)
		{
			Info = info;
			Self = init.self;
			Key = info.OrderName + "_" + Self.ActorID;
			var playeractor = Self;
			if (Self.Owner.PlayerActor != null)
				playeractor = Self.Owner.PlayerActor;
			devmode = playeractor.Trait<DeveloperMode>();
			tt = playeractor.Trait<TechTree>();
			RadarPings = Exts.Lazy(() => init.world.WorldActor.TraitOrDefault<RadarPings>());
		}

		public virtual void Charging(Actor self, string key)
		{
			Sound.PlayToPlayer(self.Owner, Info.BeginChargeSound);
		}

		public virtual void Charged(Actor self, string key)
		{
			Sound.PlayToPlayer(self.Owner, Info.EndChargeSound);
		}

		public virtual void Activate(Order order)
		{
			if (!Ready || Disabled)
				return;

			if (Info.DisplayRadarPing && RadarPings != null)
			{
				ping = RadarPings.Value.Add(
					() => order.Player.IsAlliedWith(Self.World.RenderPlayer),
					Self.World.Map.CenterOfCell(order.TargetLocation),
					order.Player.Color.RGB,
					Info.RadarPingDuration);
			}

			RemainingTime = TotalTime;
			notifiedCharging = notifiedReady = false;

			if (Info.OneShot)
				HasPrerequisites = false;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == Key)
				Activate(order);
		}

		public virtual IOrderGenerator OrderGenerator(string order)
		{
			Sound.PlayToPlayer(Self.Owner, Info.SelectTargetSound);
			return new SelectGenericPowerTarget(order, this, "ability", MouseButton.Left);
		}

		public void TargetLocation()
		{
			if (!Ready)
				return;
			
			Self.World.OrderGenerator = this.OrderGenerator(Key);
		}

		public void AddedToWorld(Actor self)
		{
			TotalTime = Info.ChargeTime * 25;
			RemainingTime = TotalTime;
			HasPrerequisites = true;

			if (Info.Prerequisites.Length != 0)
			{
				HasPrerequisites = tt.HasPrerequisites(Info.Prerequisites);
				tt.Add(Key, Info.Prerequisites, 0, this);
			}
		}

		public void RemovedFromWorld(Actor self)
		{
			tt.Remove(Key);
		}

		public void Tick(Actor self)
		{
			if (Disabled)
				return;
			if (devmode.FastCharge && RemainingTime > 25)
				RemainingTime = 25;

			if (RemainingTime > 0) --RemainingTime;
			if (!notifiedCharging)
			{
				Charging(this.Self, Key);
				notifiedCharging = true;
			}

			if (RemainingTime == 0 && !notifiedReady)
			{
				Charged(this.Self, Key);
				notifiedReady = true;
			}
		}

		public void PrerequisitesAvailable(string key)
		{
			HasPrerequisites = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			HasPrerequisites = false;
			RemainingTime = TotalTime;
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }

		public static IEnumerable<SupportPower> GetPowersForActor(Actor a)
		{
			if (!a.HasTrait<SupportPower>())
				return NoInstances;

			return a.TraitsImplementing<SupportPower>();
		}

		static readonly SupportPower[] NoInstances = { };
	}

	public class SelectGenericPowerTarget : IOrderGenerator
	{
		readonly SupportPower sp;
		readonly string order;
		readonly string cursor;
		readonly MouseButton expectedButton;

		public SelectGenericPowerTarget(string order, SupportPower sp, string cursor, MouseButton button)
		{
			this.sp = sp;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == expectedButton && world.Map.Contains(xy))
				yield return new Order(order, sp.Self, false) { TargetLocation = xy, SuppressVisualFeedback = true };
		}

		public virtual void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (sp.Disabled)
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world) { yield break; }
		public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.Contains(xy) ? cursor : "generic-blocked"; }
	}
}
