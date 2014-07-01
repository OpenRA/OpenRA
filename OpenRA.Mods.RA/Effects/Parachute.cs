#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class Parachute : IEffect
	{
		readonly ParachutableInfo parachutableInfo;
		readonly Animation parachute;
		readonly Animation shadow;
		readonly WVec parachuteOffset;
		readonly Actor cargo;
		WPos pos;
		WVec fallRate = new WVec(0, 0, 13);

		public Parachute(Actor cargo, WPos dropPosition)
		{
			this.cargo = cargo;

			parachutableInfo = cargo.Info.Traits.GetOrDefault<ParachutableInfo>();
			var parachuteSprite = parachutableInfo != null ? parachutableInfo.ParachuteSequence : null;
			if (parachuteSprite != null)
			{
				parachute = new Animation(cargo.World, parachuteSprite);
				parachute.PlayThen("open", () => parachute.PlayRepeating("idle"));
			}

			var shadowSprite = parachutableInfo != null ? parachutableInfo.ShadowSequence : null;
			if (shadowSprite != null)
			{
				shadow = new Animation(cargo.World, shadowSprite);
				shadow.PlayRepeating("idle");
			}

			if (parachutableInfo != null)
				parachuteOffset = parachutableInfo.ParachuteOffset;

			// Adjust x,y to match the target subcell
			cargo.Trait<IPositionable>().SetPosition(cargo, cargo.World.Map.CellContaining(dropPosition));
			var cp = cargo.CenterPosition;
			pos = new WPos(cp.X, cp.Y, dropPosition.Z);
		}

		public void Tick(World world)
		{
			if (parachute != null)
				parachute.Tick();

			if (shadow != null)
				shadow.Tick();

			pos -= fallRate;

			if (pos.Z <= 0)
			{
				world.AddFrameEndTask(w =>
				{
					w.Remove(this);
					cargo.CancelActivity();
					w.Add(cargo);

					foreach (var npl in cargo.TraitsImplementing<INotifyParachuteLanded>())
						npl.OnLanded();
				});
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var rc = cargo.Render(wr);

			// Don't render anything if the cargo is invisible (e.g. under fog)
			if (!rc.Any())
				yield break;

			var parachuteShadowPalette = wr.Palette(parachutableInfo.ParachuteShadowPalette);
			foreach (var c in rc)
			{
				if (!c.IsDecoration && shadow == null)
					yield return c.WithPalette(parachuteShadowPalette).WithZOffset(c.ZOffset - 1).AsDecoration();

				yield return c.OffsetBy(pos - c.Pos);
			}

			var shadowPalette = !string.IsNullOrEmpty(parachutableInfo.ShadowPalette) ? wr.Palette(parachutableInfo.ShadowPalette) : rc.First().Palette;
			if (shadow != null)
				foreach (var r in shadow.Render(pos, parachuteOffset, 1, shadowPalette, 1f))
					yield return r;

			var parachutePalette = !string.IsNullOrEmpty(parachutableInfo.ParachutePalette) ? wr.Palette(parachutableInfo.ParachutePalette) : rc.First().Palette;
			if (parachute != null)
				foreach (var r in parachute.Render(pos, parachuteOffset, 1, parachutePalette, 1f))
					yield return r;
		}
	}
}
