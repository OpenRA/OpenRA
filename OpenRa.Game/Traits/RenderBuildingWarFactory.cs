using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderWarFactory : IRender, INotifyBuildComplete, INotifyDamage, ITick, INotifyProduction
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
					Game.CellSize * (float2)self.Location, self.Owner.Palette, 2);
		}

		public void Tick(Actor self)
		{
			if (doneBuilding) roof.Tick();

			var b = self.GetBounds(false);
			if (isOpen && !Game.UnitInfluence.GetUnitsAt(((1/24f) * self.CenterLocation).ToInt2()).Any())
			{
				isOpen = false;
				roof.PlayBackwardsThen(prefix + "build-top", () => roof.Play(prefix + "idle-top"));
			}
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

		public void UnitProduced(Actor self, Actor other)
		{
			roof.PlayThen(prefix + "build-top", () => isOpen = true);
		}
	}
}
