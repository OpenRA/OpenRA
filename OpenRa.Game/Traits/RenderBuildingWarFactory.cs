using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class RenderWarFactory : RenderBuilding
	{
		public Animation roof;
		bool doneBuilding;
		bool isOpen;
		public readonly Actor self;
		string prefix = "";

		public RenderWarFactory(Actor self)
			: base(self)
		{
			this.self = self;

			roof = new Animation(self.unitInfo.Image ?? self.unitInfo.Name);
			Make( () =>
			{
				doneBuilding = true;
				anim.Play("idle");
				roof.Play(prefix + "idle-top");
			}, self);
		}

		public IEnumerable<Tuple<Sprite, float2, int>> RenderRoof(Actor self)
		{
			if (doneBuilding)
				yield return Tuple.New(roof.Image, 
					24f * (float2)self.Location, self.Owner.Palette);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (doneBuilding)
				roof.Tick();

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

		public override void Damaged(Actor self, DamageState ds)
		{
			base.Damaged(self, ds);

			switch (ds)
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
