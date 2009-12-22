using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderWarFactory : IRender, INotifyBuildComplete, INotifyDamage, ITick
	{
		public Animation roof;
		bool doneBuilding;
		bool isOpen;
		public readonly Actor self;
		string prefix = "";

		public RenderWarFactory(Actor self)
		{
			this.self = self;
			roof = new Animation(self.Info.Image ?? self.Info.Name);
		}

		public void BuildingComplete( Actor self )
		{
			doneBuilding = true;
			roof.Play( prefix + "idle-top" );
		}

		public IEnumerable<Renderable> Render(Actor self)
		{
			if (doneBuilding)
				yield return new Renderable(roof.Image, 
					24f * (float2)self.Location, self.Owner.Palette, 2);
		}

		public void Tick(Actor self)
		{
			if (doneBuilding) roof.Tick();

			var b = self.Bounds;
			if (isOpen && null == Game.UnitInfluence.GetUnitAt(((1/24f) * self.CenterLocation).ToInt2()))
			{
				isOpen = false;
				roof.PlayBackwardsThen(prefix + "build-top", () => roof.Play(prefix + "idle-top"));
			}
		}

		public void EjectUnit()
		{
			roof.PlayThen(prefix + "build-top", () => isOpen = true);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;
			switch (e.DamageState)
			{
				case DamageState.Normal:
					prefix = "";
					roof.ReplaceAnim(roof.CurrentSequence.Name.Replace("damaged-",""));
					break;
				case DamageState.Half:
					prefix = "damaged-";
					roof.ReplaceAnim("damaged-" + roof.CurrentSequence.Name);
					break;
			}
		}
	}
}
