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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class Parachute : IEffect
	{
		readonly Animation anim;
		readonly string palette;
		readonly Animation paraAnim;
		readonly PPos location;

		readonly Actor cargo;

		int2 offset;
		float altitude;
		const float fallRate = .3f;

		public Parachute(Player owner, PPos location, int altitude, Actor cargo)
		{
			this.location = location;
			this.altitude = altitude;
			this.cargo = cargo;

			var rs = cargo.Trait<RenderSimple>();
			var image = rs.anim.Name;
			palette = rs.Palette(owner);

			anim = new Animation(image);
			if (anim.HasSequence("idle"))
				anim.PlayFetchIndex("idle", () => 0);
			else
				anim.PlayFetchIndex("stand", () => 0);
			anim.Tick();

			var pai = cargo.Info.Traits.GetOrDefault<ParachuteAttachmentInfo>();

			paraAnim = new Animation(pai != null ? pai.ParachuteSprite : "parach");
			paraAnim.PlayThen("open", () => paraAnim.PlayRepeating("idle"));

			if (pai != null) offset = pai.Offset;
		}

		public void Tick(World world)
		{
			paraAnim.Tick();

			altitude -= fallRate;

			if (altitude <= 0)
				world.AddFrameEndTask(w =>
					{
						w.Remove(this);
						var loc = location.ToCPos();
						cargo.CancelActivity();
						cargo.Trait<ITeleportable>().SetPosition(cargo, loc);
						w.Add(cargo);
					});
		}

		public IEnumerable<Renderable> Render()
		{
			var pos = location.ToFloat2() - new float2(0, altitude);
			yield return new Renderable(anim.Image, location.ToFloat2() - .5f * anim.Image.size, "shadow", 0);
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, palette, 2);
			yield return new Renderable(paraAnim.Image, pos - .5f * paraAnim.Image.size + offset, palette, 3);
		}
	}
}
