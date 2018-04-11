#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class NukeLaunch : IProjectile, ISpatiallyPartitionable
	{
		readonly Player firedBy;
		readonly Animation anim;
		readonly WeaponInfo weapon;
		readonly string weaponPalette;
		readonly string downSequence;
		readonly string flashType;

		readonly WPos ascendSource;
		readonly WPos ascendTarget;
		readonly WPos descendSource;
		readonly WPos descendTarget;
		readonly int delay;
		readonly int turn;

		WPos pos;
		int ticks;
		bool isAddedToScreenMap;

		public NukeLaunch(Player firedBy, string name, WeaponInfo weapon, string weaponPalette, string upSequence, string downSequence,
			WPos launchPos, WPos targetPos, WDist velocity, int delay, bool skipAscent, string flashType)
		{
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.weaponPalette = weaponPalette;
			this.downSequence = downSequence;
			this.delay = delay;
			turn = skipAscent ? 0 : delay / 2;
			this.flashType = flashType;

			var offset = new WVec(WDist.Zero, WDist.Zero, velocity * (delay - turn));
			ascendSource = launchPos;
			ascendTarget = launchPos + offset;
			descendSource = targetPos + offset;
			descendTarget = targetPos;

			anim = new Animation(firedBy.World, name);
			anim.PlayRepeating(upSequence);

			pos = skipAscent ? descendSource : ascendSource;
			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(SoundType.World, weapon.Report.Random(firedBy.World.SharedRandom), pos);
		}

		public void Tick(World world)
		{
			if (!isAddedToScreenMap)
			{
				world.ScreenMap.Add(this, pos, anim.Image);
				isAddedToScreenMap = true;
			}

			anim.Tick();

			if (ticks == turn)
				anim.PlayRepeating(downSequence);

			if (ticks < turn)
				pos = WPos.LerpQuadratic(ascendSource, ascendTarget, WAngle.Zero, ticks, turn);
			else
				pos = WPos.LerpQuadratic(descendSource, descendTarget, WAngle.Zero, ticks - turn, delay - turn);

			if (ticks == delay)
				Explode(world);

			world.ScreenMap.Update(this, pos, anim.Image);

			ticks++;
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
			weapon.Impact(Target.FromPos(pos), firedBy.PlayerActor, Enumerable.Empty<int>());
			world.WorldActor.Trait<ScreenShaker>().AddEffect(20, pos, 5);

			foreach (var flash in world.WorldActor.TraitsImplementing<FlashPaletteEffect>())
				if (flash.Info.Type == flashType)
					flash.Enable(-1);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette(weaponPalette));
		}

		public float FractionComplete { get { return ticks * 1f / delay; } }
	}
}
