#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class Parachute : IEffect
	{
		readonly Animation anim;
		readonly Animation paraAnim;
		readonly float2 location;
		
		readonly Actor cargo;
		readonly Player owner;

		float altitude;
		const float fallRate = .3f;

		public Parachute(Player owner, string image, float2 location, int altitude, Actor cargo)
		{
			this.location = location;
			this.altitude = altitude;
			this.cargo = cargo;
			this.owner = owner;

			anim = new Animation(image);
			if (anim.HasSequence("idle"))
				anim.PlayFetchIndex("idle", () => 0);
			else
				anim.PlayFetchIndex("stand", () => 0);
			anim.Tick();

			paraAnim = new Animation("parach");
			paraAnim.PlayThen("open", () => paraAnim.PlayRepeating("idle"));
		}

		public void Tick(World world)
		{ 
			paraAnim.Tick();

			altitude -= fallRate;

			if (altitude <= 0)
				world.AddFrameEndTask(w =>
					{
						w.Remove(this);
						var loc = Traits.Util.CellContaining(location);
						cargo.CancelActivity();
						
						var mobile = cargo.traits.WithInterface<IMove>().FirstOrDefault();

						if (mobile != null)
							mobile.SetPosition(cargo, loc);
						else
						{
							cargo.CenterLocation = Traits.Util.CenterOfCell(loc);

							if (cargo.traits.Contains<IOccupySpace>())
								world.WorldActor.traits.Get<UnitInfluence>().Add(cargo, cargo.traits.Get<IOccupySpace>());
						}
						w.Add(cargo);
					});
		}

		public IEnumerable<Renderable> Render()
		{
			var pos = location - new float2(0, altitude);
			yield return new Renderable(anim.Image, location - .5f * anim.Image.size, "shadow", 0);
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, owner.Palette, 2);
			yield return new Renderable(paraAnim.Image, pos - .5f * paraAnim.Image.size, owner.Palette, 3);
		}
	}
}
