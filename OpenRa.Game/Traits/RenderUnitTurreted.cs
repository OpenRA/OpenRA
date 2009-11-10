using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class RenderUnitTurreted : RenderUnit
	{
		public Animation turretAnim;
		public Animation muzzleFlash;
		public float primaryRecoil = 0.0f, secondaryRecoil = 0.0f;

		public RenderUnitTurreted(Actor self)
			: base(self)
		{
			turretAnim = new Animation(self.unitInfo.Name);
			if (self.traits.Contains<Turreted>())
			{
				if (self.unitInfo.MuzzleFlash)
				{
					muzzleFlash = new Animation(self.unitInfo.Name);
					muzzleFlash.PlayFetchIndex("muzzle",
						() => (Util.QuantizeFacing(self.traits.Get<Turreted>().turretFacing,8)) * 6 + (int)(primaryRecoil * 6));
				}

				turretAnim.PlayFetchIndex("turret",
					() => self.traits.Get<Turreted>().turretFacing / 8);
			}
			else
				turretAnim.PlayRepeating("turret");		/* not really a turret; it's a spinner */
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();

			yield return Util.Centered(self, anim.Image, self.CenterLocation);
			yield return Util.Centered(self, turretAnim.Image, self.CenterLocation 
				+ Util.GetTurretPosition(self, self.unitInfo.PrimaryOffset, primaryRecoil));
			if (self.unitInfo.SecondaryOffset != null)
				yield return Util.Centered(self, turretAnim.Image, self.CenterLocation
					+ Util.GetTurretPosition(self, self.unitInfo.SecondaryOffset, secondaryRecoil));

			if (muzzleFlash != null && primaryRecoil > 0)
				yield return Util.Centered(self, muzzleFlash.Image, self.CenterLocation
				+ Util.GetTurretPosition(self, self.unitInfo.PrimaryOffset, primaryRecoil));
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			turretAnim.Tick();
			primaryRecoil = Math.Max(0f, primaryRecoil - .2f);
			secondaryRecoil = Math.Max(0f, secondaryRecoil - .2f);
			if (muzzleFlash != null)
				muzzleFlash.Tick();
		}
	}
}
