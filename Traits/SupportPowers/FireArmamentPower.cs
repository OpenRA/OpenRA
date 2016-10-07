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
	[Desc("Support power type to fire a burst of armaments.")]
	public class FireArmamentPowerInfo : SupportPowerInfo
	{
		[Desc("The `Name` of the armaments this support power is allowed to fire.")]
		public readonly string ArmamentName = "superweapon";

		[Desc("If `AllowMultiple` is `false`, how many instances of this support power are allowed to fire.",
		      "Actual instances might end up less due to range/etc.")]
		public readonly int MaximumFiringInstances = 1;

		[Desc("Amount of time before detonation to remove the beacon.")]
		public readonly int BeaconRemoveAdvance = 25;

		[ActorReference]
		[Desc("Actor to spawn before firing.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time before firing to spawn the camera.")]
		public readonly int CameraSpawnAdvance = 25;

		[Desc("Amount of time after firing to remove the camera.")]
		public readonly int CameraRemoveDelay = 25;

		public override object Create(ActorInitializer init) { return new FireArmamentPower(init.Self, this); }
	}

	public class FireArmamentPower : SupportPower, ITick, INotifyBurstComplete, INotifyCreated
	{
		public readonly FireArmamentPowerInfo FireArmamentPowerInfo;

		IFacing facing;
		HashSet<Armament> activeArmaments;
		HashSet<Turreted> turrets;

		bool enabled;
		int ticks;
		int estimatedTicks;
		Target target;

		public Armament[] Armaments;

		public FireArmamentPower(Actor self, FireArmamentPowerInfo info)
			: base(self, info)
		{
			FireArmamentPowerInfo = info;
			enabled = false;
		}

		void INotifyCreated.Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();
			Armaments = self.TraitsImplementing<Armament>().Where(t => t.Info.Name.Contains(FireArmamentPowerInfo.ArmamentName)).ToArray();
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			activeArmaments = Armaments.Where(x => !x.IsTraitDisabled).ToHashSet();

			var armamentturrets = activeArmaments.Select(x => x.Info.Turret).ToHashSet();

			// TODO: Fix this when upgradable Turreteds arrive.
			turrets = self.TraitsImplementing<Turreted>().Where(x => armamentturrets.Contains(x.Name)).ToHashSet();

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Game.Sound.Play(FireArmamentPowerInfo.LaunchSound);
			else
				Game.Sound.Play(FireArmamentPowerInfo.IncomingSound);

			target = Target.FromCell(self.World, order.TargetLocation);

			enabled = true;

			// TODO: Estimate the projectile travel time somehow
			estimatedTicks = activeArmaments.Max(x => x.FireDelay);

			if (FireArmamentPowerInfo.CameraActor != null)
			{
				var camera = self.World.CreateActor(false, FireArmamentPowerInfo.CameraActor, new TypeDictionary
				                                    {
					new LocationInit(order.TargetLocation),
					new OwnerInit(self.Owner),
				});

				camera.QueueActivity(new Wait(FireArmamentPowerInfo.CameraSpawnAdvance + FireArmamentPowerInfo.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());

				Action addCamera = () => self.World.AddFrameEndTask(w => w.Add(camera));
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(estimatedTicks - FireArmamentPowerInfo.CameraSpawnAdvance, addCamera)));
			}

			if (FireArmamentPowerInfo.DisplayBeacon)
			{
				var beacon = new Beacon(
					order.Player,
					self.World.Map.CenterOfCell(order.TargetLocation),
					FireArmamentPowerInfo.BeaconPaletteIsPlayerPalette,
					FireArmamentPowerInfo.BeaconPalette,
					FireArmamentPowerInfo.BeaconImage,
					FireArmamentPowerInfo.BeaconPoster,
					FireArmamentPowerInfo.BeaconPosterPalette,
					FireArmamentPowerInfo.ArrowSequence,
					FireArmamentPowerInfo.CircleSequence,
					FireArmamentPowerInfo.ClockSequence,
					() => FractionComplete);

				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
				                                                       {
					w.Remove(beacon);
					beacon = null;
				});

				self.World.AddFrameEndTask(w =>
				                           {
					w.Add(beacon);
					w.Add(new DelayedAction(estimatedTicks - FireArmamentPowerInfo.BeaconRemoveAdvance, removeBeacon));
				});
			}

			ticks = 0;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, FireArmamentPowerInfo.SelectTargetSound);
			self.World.OrderGenerator = new SelectArmamentPowerTarget(self, order, manager, this);
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
		readonly FireArmamentPower power;

		readonly IEnumerable<Tuple<Actor, WDist, WDist>> instances;

		public SelectArmamentPowerTarget(Actor self, string order, SupportPowerManager manager, FireArmamentPower power)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.self = self;
			this.manager = manager;
			this.order = order;
			this.power = power;

			instances = GetActualInstances(self, power);
		}

		IEnumerable<Tuple<Actor, WDist, WDist>> GetActualInstances(Actor self, FireArmamentPower power)
		{
			if (!power.FireArmamentPowerInfo.AllowMultiple)
			{
				var actorswithpower = self.World.ActorsWithTrait<FireArmamentPower>()
					.Where(x => x.Actor.Owner == self.Owner && x.Trait.FireArmamentPowerInfo.OrderName.Contains(power.FireArmamentPowerInfo.OrderName));
				foreach (var a in actorswithpower)
				{
					yield return Tuple.Create(a.Actor,
						a.Trait.Armaments.Where(x => !x.IsTraitDisabled).Min(x => x.Weapon.MinRange),
						a.Trait.Armaments.Where(x => !x.IsTraitDisabled).Max(x => x.Weapon.Range));
				}
			}
			else
			{
				yield return Tuple.Create(self,
					power.Armaments.Where(x => !x.IsTraitDisabled).Min(a => a.Weapon.MinRange),
					power.Armaments.Where(x => !x.IsTraitDisabled).Max(a => a.Weapon.Range));
			}

			yield break;
		}

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos xy, int2 worldpixel, MouseInput mi)
		{
			var pos = world.Map.CenterOfCell(xy);

			world.CancelInputMode();
			if (mi.Button == MouseButton.Left && IsValidTargetCell(xy))
			{
				var actors = instances.Where(x => !x.Item1.IsDisabled() && (x.Item1.CenterPosition - pos).HorizontalLengthSquared < x.Item3.LengthSquared)
					.OrderBy(x => (x.Item1.CenterPosition - pos).HorizontalLengthSquared).Select(x => x.Item1).Take(power.FireArmamentPowerInfo.MaximumFiringInstances);

				foreach (var a in actors)
				{
					yield return new Order(order, manager.Self, false) { TargetLocation = xy, SuppressVisualFeedback = true };
				}
			}
		}

		public virtual void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			foreach (var i in instances)
			{
				if (!i.Item1.IsDisabled())
				{
					yield return new RangeCircleRenderable(
						i.Item1.CenterPosition,
						i.Item2,
						0,
						Color.Red,
						Color.FromArgb(96, Color.Black));

					yield return new RangeCircleRenderable(
						i.Item1.CenterPosition,
						i.Item3,
						0,
						Color.Red,
						Color.FromArgb(96, Color.Black));
				}
			}

			yield break;
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return IsValidTargetCell(cell) ? power.FireArmamentPowerInfo.Cursor : "generic-blocked";
		}

		bool IsValidTargetCell(CPos xy)
		{
			if (!self.World.Map.Contains(xy))
				return false;

			var tc = Target.FromCell(self.World, xy);

			return instances.Any(x => !x.Item1.IsDisabled() && tc.IsInRange(x.Item1.CenterPosition, x.Item3) && !tc.IsInRange(x.Item1.CenterPosition, x.Item2));
		}
	}
}