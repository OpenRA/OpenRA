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
		readonly Animation paraAnim;
		readonly WVec parachuteOffset;
		readonly Actor cargo;
		readonly Animation paraShadow;
		WPos pos;
		WVec fallRate = new WVec(0, 0, 13);

		public Parachute(Actor cargo, WPos dropPosition)
		{
			this.cargo = cargo;

			var pai = cargo.Info.Traits.GetOrDefault<ParachuteAttachmentInfo>();
			paraAnim = new Animation(cargo.World, pai != null ? pai.ParachuteSprite : "parach");
			paraAnim.PlayThen("open", () => paraAnim.PlayRepeating("idle"));

			paraShadow = new Animation("parach-shadow");
			paraShadow.PlayRepeating("idle");

			if (pai != null)
				parachuteOffset = pai.Offset;

			// Adjust x,y to match the target subcell
			cargo.Trait<IPositionable>().SetPosition(cargo, dropPosition.ToCPos());
			var cp = cargo.CenterPosition;
			pos = new WPos(cp.X, cp.Y, dropPosition.Z);
		}

		public void Tick(World world)
		{
			paraAnim.Tick();

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

			var shadow = wr.Palette("shadow");
			foreach (var c in rc)
			{
				if (!c.IsDecoration)
					foreach (var r in paraShadow.Render(c.Pos, shadow))
						yield return r;

				yield return c.OffsetBy(pos - c.Pos);
			}

			foreach (var r in paraAnim.Render(pos, parachuteOffset, 1, rc.First().Palette, 1f))
				yield return r;
		}
	}
}
