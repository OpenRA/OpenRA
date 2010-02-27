#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
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
						w.Add(cargo);
						cargo.CancelActivity();
						cargo.traits.Get<Mobile>().TeleportTo(cargo, ((1 / 24f) * location).ToInt2());
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
