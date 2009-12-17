using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamageEx
	{
		public RenderInfantry(Actor self)
			: base(self)
		{
			anim.PlayFacing("stand", 
				() => self.traits.Get<Unit>().Facing);
		}

		bool ChooseMoveAnim(Actor self)
		{
			if (!(self.GetCurrentActivity() is Activities.Move))
				return false;

			var mobile = self.traits.Get<Mobile>();
			if (float2.WithinEpsilon(self.CenterLocation, Util.CenterOfCell(mobile.toCell), 2)) return false;
			var dir = Util.QuantizeFacing(self.traits.Get<Unit>().Facing, 8);

			if (anim.CurrentSequence.Name.StartsWith("run-"))
				anim.ReplaceAnim("run-" + dir);
			else
				anim.PlayRepeating("run-" + dir);

			return true;
		}

		bool inAttack = false;

		public void Attacking(Actor self)
		{
			var dir = Util.QuantizeFacing(self.traits.Get<Unit>().Facing, 8);
			inAttack = true;
			anim.PlayThen("shoot-" + dir, () => inAttack = false);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (inAttack) return;
			if (ChooseMoveAnim(self)) return;

			/* todo: idle anims, etc */

			anim.PlayFacing("stand",
				() => self.traits.Get<Unit>().Facing);
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			yield return Util.Centered(self, anim.Image, self.CenterLocation);
		}

		public void Damaged(Actor self, int damage, WarheadInfo warhead)
		{
			if (self.Health <= 0)
				Game.world.AddFrameEndTask(w => w.Add(new Corpse(self, warhead.InfDeath)));
		}

		public void Damaged(Actor self, DamageState ds) {}
	}
}
