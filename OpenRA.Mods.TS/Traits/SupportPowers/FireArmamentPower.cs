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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	class FireArmamentPowerInfo : SupportPowerInfo
	{
		[Desc("Armament names")]
		public readonly string ArmamentName = "superweapon";

		[Desc("Amount of time before detonation to remove the beacon")]
		public readonly int BeaconRemoveAdvance = 25;

		[ActorReference]
		[Desc("Actor to spawn before firing")]
		public readonly string CameraActor = null;

		[Desc("Amount of time before firing to spawn the camera")]
		public readonly int CameraSpawnAdvance = 25;

		[Desc("Amount of time after firing to remove the camera")]
		public readonly int CameraRemoveDelay = 25;

		public override object Create(ActorInitializer init) { return new FireArmamentPower(init.Self, this); }
	}

	class FireArmamentPower : SupportPower, ITick, INotifyFiredSalvo
	{
		readonly FireArmamentPowerInfo info;

		protected Lazy<IFacing> facing;
		protected Lazy<Armament> armament;

		bool enabled;
		int ticks;
		int estimatedTicks;
		Target target;

		public FireArmamentPower(Actor self, FireArmamentPowerInfo info)
			: base(self, info)
		{
			armament = Exts.Lazy(() => self.TraitsImplementing<Armament>().FirstOrDefault(t => t.Info.Name == info.ArmamentName));
			facing = Exts.Lazy(() => self.TraitOrDefault<IFacing>());
			this.info = info;
			enabled = false;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Game.Sound.Play(Info.LaunchSound);
			else
				Game.Sound.Play(Info.IncomingSound);

			target = Target.FromCell(self.World, order.TargetLocation);

			enabled = true;

			// TODO: Estimate the projectile travel time somehow
			estimatedTicks = armament.Value.FireDelay;

			if (info.CameraActor != null)
			{
				var camera = self.World.CreateActor(false, info.CameraActor, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(self.Owner),
					});

				camera.QueueActivity(new Wait(info.CameraSpawnAdvance + info.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());

				Action addCamera = () => self.World.AddFrameEndTask(w => w.Add(camera));
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(estimatedTicks - info.CameraSpawnAdvance, addCamera)));
			}

			if (Info.DisplayBeacon)
			{
				var beacon = new Beacon(
					order.Player,
					self.World.Map.CenterOfCell(order.TargetLocation),
					Info.BeaconPalettePrefix,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					() => FractionComplete);

				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
					{
						w.Remove(beacon);
						beacon = null;
					});

				self.World.AddFrameEndTask(w =>
					{
						w.Add(beacon);
						w.Add(new DelayedAction(estimatedTicks - info.BeaconRemoveAdvance, removeBeacon));
					});
			}

			ticks = 0;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectArmamentPowerTarget(self, order, manager, armament.Value);
		}

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			if (armament.Value.Turret.Value != null && !armament.Value.Turret.Value.FaceTarget(self, target))
				return;

			armament.Value.CheckFire(self, facing.Value, target);

			ticks++;
		}

		public void FiredSalvo(IArmamentInfo ai)
		{
			if (ai == armament.Value.Info)
				enabled = false;
		}

		float FractionComplete { get { return ticks * 1f / estimatedTicks; } }
	}

	public class SelectArmamentPowerTarget : IOrderGenerator
	{
		readonly Actor self;
		readonly SupportPowerManager manager;
		readonly string order;
		readonly Armament armament;

		public SelectArmamentPowerTarget(Actor self, string order, SupportPowerManager manager, Armament armament)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.self = self;
			this.manager = manager;
			this.order = order;
			this.armament = armament;
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == MouseButton.Left && IsValidTargetCell(xy))
				yield return new Order(order, manager.Self, false) { TargetLocation = xy, SuppressVisualFeedback = true };
		}

		public virtual void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
		{
			yield return new RangeCircleRenderable(
				self.CenterPosition,
				armament.Weapon.MinRange,
				0,
				Color.Red,
				Color.FromArgb(96, Color.Black));

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				armament.Weapon.Range,
				0,
				Color.Red,
				Color.FromArgb(96, Color.Black));

			yield break;
		}

		public string GetCursor(World world, CPos xy, MouseInput mi) { return IsValidTargetCell(xy) ? armament.Info.Cursor : "generic-blocked"; }

		bool IsValidTargetCell(CPos xy)
		{
			if (!self.World.Map.Contains(xy))
				return false;

			var tc = Target.FromCell(self.World, xy);

			return tc.IsInRange(self.CenterPosition, armament.Weapon.Range) && !tc.IsInRange(self.CenterPosition, armament.Weapon.MinRange);
		}
	}
}