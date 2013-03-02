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
		readonly PPos location;

		readonly Actor cargo;

		int2 offset;
		float altitude;
		const float fallRate = .3f;

		public Parachute(Actor cargo, PPos location, int altitude)
		{
			this.location = location;
			this.altitude = altitude;
			this.cargo = cargo;

			var pai = cargo.Info.Traits.GetOrDefault<ParachuteAttachmentInfo>();
			paraAnim = new Animation(pai != null ? pai.ParachuteSprite : "parach");
			paraAnim.PlayThen("open", () => paraAnim.PlayRepeating("idle"));

			if (pai != null) offset = pai.Offset;

			cargo.Trait<ITeleportable>().SetPxPosition(cargo, location);
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

						foreach( var npl in cargo.TraitsImplementing<INotifyParachuteLanded>() )
							npl.OnLanded();
					});
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			var rc = cargo.Render(wr).Select(a => a.WithPos(a.Pos - new float2(0, altitude))
			                                    .WithZOffset(a.ZOffset + (int)altitude));

			// Don't render anything if the cargo is invisible (e.g. under fog)
			if (!rc.Any())
				yield break;

			foreach (var c in rc)
			{
				yield return c.WithPos(location.ToFloat2() - .5f * c.Sprite.size).WithPalette(wr.Palette("shadow")).WithZOffset(0);
				yield return c.WithZOffset(2);
			}

			var pos = location.ToFloat2() - new float2(0, altitude);
			yield return new Renderable(paraAnim.Image, pos - .5f * paraAnim.Image.size + offset, rc.First().Palette, 3);
		}
	}
}
