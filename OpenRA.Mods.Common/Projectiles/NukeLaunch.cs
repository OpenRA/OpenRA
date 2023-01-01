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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class NukeLaunch : IProjectile, ISpatiallyPartitionable
	{
		readonly Player firedBy;
		readonly Animation anim;
		readonly WeaponInfo weapon;
		readonly string weaponPalette;
		readonly string upSequence;
		readonly string downSequence;

		readonly WPos ascendSource;
		readonly WPos ascendTarget;
		readonly WPos descendSource;
		readonly WPos descendTarget;
		readonly WDist detonationAltitude;
		readonly bool removeOnDetonation;
		readonly int impactDelay;
		readonly int turn;
		readonly string trailImage;
		readonly string[] trailSequences;
		readonly string trailPalette;
		readonly int trailInterval;
		readonly int trailDelay;

		WPos pos;
		int ticks, trailTicks;
		int launchDelay;
		bool isLaunched;
		bool detonated;

		public NukeLaunch(Player firedBy, string image, WeaponInfo weapon, string weaponPalette, string upSequence, string downSequence,
			WPos launchPos, WPos targetPos, WDist detonationAltitude, bool removeOnDetonation, WDist velocity, int launchDelay, int impactDelay,
			bool skipAscent,
			string trailImage, string[] trailSequences, string trailPalette, bool trailUsePlayerPalette, int trailDelay, int trailInterval)
		{
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.weaponPalette = weaponPalette;
			this.upSequence = upSequence;
			this.downSequence = downSequence;
			this.launchDelay = launchDelay;
			this.impactDelay = impactDelay;
			turn = skipAscent ? 0 : impactDelay / 2;
			this.trailImage = trailImage;
			this.trailSequences = trailSequences;
			this.trailPalette = trailPalette;
			if (trailUsePlayerPalette)
				this.trailPalette += firedBy.InternalName;

			this.trailInterval = trailInterval;
			this.trailDelay = trailDelay;
			trailTicks = trailDelay;

			var offset = new WVec(WDist.Zero, WDist.Zero, velocity * (impactDelay - turn));
			ascendSource = launchPos;
			ascendTarget = launchPos + offset;
			descendSource = targetPos + offset;
			descendTarget = targetPos;
			this.detonationAltitude = detonationAltitude;
			this.removeOnDetonation = removeOnDetonation;

			if (!string.IsNullOrEmpty(image))
				anim = new Animation(firedBy.World, image);

			pos = skipAscent ? descendSource : ascendSource;
		}

		public void Tick(World world)
		{
			if (launchDelay-- > 0)
				return;

			if (!isLaunched)
			{
				if (weapon.Report != null && weapon.Report.Length > 0)
					Game.Sound.Play(SoundType.World, weapon.Report, world, pos);

				if (anim != null)
				{
					anim.PlayRepeating(upSequence);
					world.ScreenMap.Add(this, pos, anim.Image);
				}

				isLaunched = true;
			}

			if (anim != null)
			{
				anim.Tick();

				if (ticks == turn)
					anim.PlayRepeating(downSequence);
			}

			var isDescending = ticks >= turn;
			if (!isDescending)
				pos = WPos.LerpQuadratic(ascendSource, ascendTarget, WAngle.Zero, ticks, turn);
			else
				pos = WPos.LerpQuadratic(descendSource, descendTarget, WAngle.Zero, ticks - turn, impactDelay - turn);

			if (!string.IsNullOrEmpty(trailImage) && --trailTicks < 0)
			{
				var trailPos = !isDescending ? WPos.LerpQuadratic(ascendSource, ascendTarget, WAngle.Zero, ticks - trailDelay, turn)
					: WPos.LerpQuadratic(descendSource, descendTarget, WAngle.Zero, ticks - turn - trailDelay, impactDelay - turn);

				world.AddFrameEndTask(w => w.Add(new SpriteEffect(trailPos, w, trailImage, trailSequences.Random(world.SharedRandom),
					trailPalette)));

				trailTicks = trailInterval;
			}

			var dat = world.Map.DistanceAboveTerrain(pos);
			if (ticks == impactDelay || (isDescending && dat <= detonationAltitude))
				Explode(world, ticks == impactDelay || removeOnDetonation);

			if (anim != null)
				world.ScreenMap.Update(this, pos, anim.Image);

			ticks++;
		}

		void Explode(World world, bool removeProjectile)
		{
			if (removeProjectile)
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });

			if (detonated)
				return;

			var target = Target.FromPos(pos);
			var warheadArgs = new WarheadArgs
			{
				Weapon = weapon,
				Source = target.CenterPosition,
				SourceActor = firedBy.PlayerActor,
				WeaponTarget = target
			};

			weapon.Impact(target, warheadArgs);

			detonated = true;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!isLaunched || anim == null)
				return Enumerable.Empty<IRenderable>();

			return anim.Render(pos, wr.Palette(weaponPalette));
		}

		public float FractionComplete => ticks * 1f / impactDelay;
	}
}
