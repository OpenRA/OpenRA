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

		public RenderWarFactory(Actor self)
			: base(self)
		{
			this.self = self;

			roof = new Animation(self.unitInfo.Image ?? self.unitInfo.Name);
			anim.PlayThen("make", () =>
			{
				doneBuilding = true;
				anim.Play("idle");
				roof.Play("idle-top");
			});
		}

		public IEnumerable<Pair<Sprite, float2>> RenderRoof(Actor self)
		{
			if (doneBuilding)
				yield return Pair.New(roof.Image, 24f * (float2)self.Location);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			roof.Tick();

			var b = self.Bounds;
			if (isOpen && !Game.SelectUnitsInBox(
				new float2(b.Left, b.Top),
				new float2(b.Right, b.Bottom)).Any(a => a.traits.Contains<Mobile>()))
			{
				isOpen = false;
				roof.PlayBackwardsThen("build-top", () => roof.Play("idle-top"));
			}
		}

		public void EjectUnit()
		{
			/* todo: hold the door open */

			roof.PlayThen("build-top", () => isOpen = true);
		}
	}
}
