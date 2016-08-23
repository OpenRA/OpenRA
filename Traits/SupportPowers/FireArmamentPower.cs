#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
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

	class FireArmamentPower : SupportPower, ITick, INotifyBurstComplete, INotifyCreated
	{
		readonly FireArmamentPowerInfo info;

		IFacing facing;
		Armament[] armaments;
		HashSet<Armament> activeArmaments;
		HashSet<Turreted> turrets;

		bool enabled;
		int ticks;
		int estimatedTicks;
		Target target;

		public FireArmamentPower(Actor self, FireArmamentPowerInfo info)
			: base(self, info)
		{
			this.info = info;
			enabled = false;
		}

		void INotifyCreated.Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();
			armaments = self.TraitsImplementing<Armament>().Where(t => t.Info.Name.Contains(info.ArmamentName)).ToArray();
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
			estimatedTicks = activeArmaments.Max(x => x.FireDelay);

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
					Info.BeaconPaletteIsPlayerPalette,
					Info.BeaconPalette,
					Info.BeaconImage,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					Info.ArrowSequence,
					Info.CircleSequence,
					Info.ClockSequence,
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
			activeArmaments = armaments.Where(x => !x.IsTraitDisabled).ToHashSet();

			var armamentturrets = activeArmaments.Select(x => x.Info.Turret).ToHashSet();

			// TODO: Fix this when upgradable Turreteds arrive.
			turrets = self.TraitsImplementing<Turreted>().Where(x => armamentturrets.Contains(x.Name)).ToHashSet();

			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectArmamentPowerTarget(self, order, manager, activeArmaments.First());
		}

		void ITick.Tick(Actor self)
		{
			if (!enabled)
				return;

			foreach (var t in turrets)
			{
				if (!t.FaceTarget(self, target))
					return;
			}

			foreach (var a in activeArmaments) {
				a.CheckFire(self, facing, target);
			}

			ticks++;

			if (!activeArmaments.Any())
				enabled = false;
		}

		void INotifyBurstComplete.FiredBurst(Actor self, Target target, Armament a)
		{
			self.World.AddFrameEndTask(w => activeArmaments.Remove(a));
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

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos xy, int2 worldpixel, MouseInput mi)
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

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return IsValidTargetCell(cell) ? armament.Info.Cursor : "generic-blocked";
		}

		bool IsValidTargetCell(CPos xy)
		{
			if (!self.World.Map.Contains(xy))
				return false;

			var tc = Target.FromCell(self.World, xy);

			return tc.IsInRange(self.CenterPosition, armament.Weapon.Range) && !tc.IsInRange(self.CenterPosition, armament.Weapon.MinRange);
		}
	}
}