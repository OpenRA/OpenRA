using System.Collections.Generic;
using OpenRa.Effects;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Mods.RA.Effects
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
			yield return new Renderable(anim.Image, location - .5f * anim.Image.size, PaletteType.Shadow, 0);
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, owner.Palette, 2);
			yield return new Renderable(paraAnim.Image, pos - .5f * paraAnim.Image.size, owner.Palette, 3);
		}
	}
}
