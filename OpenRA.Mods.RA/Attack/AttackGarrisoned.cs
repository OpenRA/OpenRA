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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class FirePort
	{
		public WVec Offset;
		public WAngle Yaw;
		public WAngle Cone;
	}

	[Desc("Cargo can fire their weapons out of fire ports.")]
	public class AttackGarrisonedInfo : AttackFollowInfo, Requires<CargoInfo>
	{
		[Desc("Fire port offsets in local coordinates.")]
		public readonly WVec[] PortOffsets = {};

		[Desc("Fire port yaw angles.")]
		public readonly WAngle[] PortYaws = {};

		[Desc("Fire port yaw cone angle.")]
		public readonly WAngle[] PortCones = {};

		public readonly string MuzzlePalette = "effect";

		public override object Create(ActorInitializer init) { return new AttackGarrisoned(init.self, this); }
	}

	public class AttackGarrisoned : AttackFollow, INotifyPassengerEntered, INotifyPassengerExited, IRender
	{
		public readonly FirePort[] Ports;

		AttackGarrisonedInfo info;
		Lazy<IBodyOrientation> coords;
		List<Armament> armaments;
		List<AnimationWithOffset> muzzles;
		Dictionary<Actor, IFacing> paxFacing;
		Dictionary<Actor, IPositionable> paxPos;
		Dictionary<Actor, RenderSprites> paxRender;


		public AttackGarrisoned(Actor self, AttackGarrisonedInfo info)
			: base(self, info)
		{
			this.info = info;
			coords = Exts.Lazy(() => self.Trait<IBodyOrientation>());
			armaments = new List<Armament>();
			muzzles = new List<AnimationWithOffset>();
			paxFacing = new Dictionary<Actor, IFacing>();
			paxPos = new Dictionary<Actor, IPositionable>();
			paxRender = new Dictionary<Actor, RenderSprites>();

			GetArmaments = () => armaments;


			if (info.PortOffsets.Length == 0)
				throw new InvalidOperationException("PortOffsets must have at least one entry.");

			if (info.PortYaws.Length != info.PortOffsets.Length)
				throw new InvalidOperationException("PortYaws must define an angle for each port.");

			if (info.PortCones.Length != info.PortOffsets.Length)
				throw new InvalidOperationException("PortCones must define an angle for each port.");

			var p = new List<FirePort>();
			for (var i = 0; i < info.PortOffsets.Length; i++)
			{
				p.Add(new FirePort
				{
					Offset = info.PortOffsets[i],
					Yaw = info.PortYaws[i],
					Cone = info.PortCones[i],
				});
			}

			Ports = p.ToArray();
		}

		public void PassengerEntered(Actor self, Actor passenger)
		{
			paxFacing.Add(passenger, passenger.Trait<IFacing>());
			paxPos.Add(passenger, passenger.Trait<IPositionable>());
			paxRender.Add(passenger, passenger.Trait<RenderSprites>());
			armaments = armaments.Append(passenger.TraitsImplementing<Armament>()
				.Where(a => info.Armaments.Contains(a.Info.Name))
				.ToArray()).ToList();
		}

		public void PassengerExited(Actor self, Actor passenger)
		{
			paxFacing.Remove(passenger);
			paxPos.Remove(passenger);
			paxRender.Remove(passenger);
			armaments.RemoveAll(a => a.Actor == passenger);
		}


		FirePort SelectFirePort(Actor self, WAngle targetYaw)
		{
			// Pick a random port that faces the target
			var bodyYaw = facing.Value != null ? WAngle.FromFacing(facing.Value.Facing) : WAngle.Zero;
			var indices = Exts.MakeArray(Ports.Length, i => i).Shuffle(self.World.SharedRandom);
			foreach (var i in indices)
			{
				var yaw = bodyYaw + Ports[i].Yaw;
				var leftTurn = (yaw - targetYaw).Angle;
				var rightTurn = (targetYaw - yaw).Angle;
				if (Math.Min(leftTurn, rightTurn) <= Ports[i].Cone.Angle)
					return Ports[i];
			}

			return null;
		}

		WVec PortOffset(Actor self, FirePort p)
		{
			var bodyOrientation = coords.Value.QuantizeOrientation(self, self.Orientation);
			return coords.Value.LocalToWorld(p.Offset.Rotate(bodyOrientation));
		}

		public override void DoAttack(Actor self, Target target)
		{
			if (!CanAttack(self, target))
				return;

			var pos = self.CenterPosition;
			var targetYaw = WAngle.FromFacing(OpenRA.Traits.Util.GetFacing(target.CenterPosition - self.CenterPosition, 0));

			foreach (var a in Armaments)
			{
				var port = SelectFirePort(self, targetYaw);
				if (port == null)
					return;

				var muzzleFacing = targetYaw.Angle / 4;
				paxFacing[a.Actor].Facing = muzzleFacing;
				paxPos[a.Actor].SetVisualPosition(a.Actor, pos + PortOffset(self, port));

				var barrel = a.CheckFire(a.Actor, facing.Value, target);
				if (barrel != null && a.Info.MuzzleSequence != null)
				{
					// Muzzle facing is fixed once the firing starts
					var muzzleAnim = new Animation(self.World, paxRender[a.Actor].GetImage(a.Actor), () => muzzleFacing);
					var sequence = a.Info.MuzzleSequence;

					if (a.Info.MuzzleSplitFacings > 0)
						sequence += OpenRA.Traits.Util.QuantizeFacing(muzzleFacing, a.Info.MuzzleSplitFacings).ToString();

					var muzzleFlash = new AnimationWithOffset(muzzleAnim,
						() => PortOffset(self, port),
						() => false,
						() => false,
						p => WithTurret.ZOffsetFromCenter(self, p, 1024));

					muzzles.Add(muzzleFlash);
					muzzleAnim.PlayThen(sequence, () => muzzles.Remove(muzzleFlash));
				}
				foreach (var npa in self.TraitsImplementing<INotifyAttack>())
					npa.Attacking(self, target, a, barrel);
			}
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			var pal = wr.Palette(info.MuzzlePalette);
			// Display muzzle flashes
			foreach (var m in muzzles)
				foreach (var r in m.Render(self, wr, pal, 1f))
					yield return r;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			// Take a copy so that Tick() can remove animations
			foreach (var m in muzzles.ToList())
				m.Animation.Tick();
		}
	}
}
