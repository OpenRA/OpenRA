using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamage
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

			var prefix = IsProne(self) ? "crawl-" : "run-";

			if (anim.CurrentSequence.Name.StartsWith(prefix))
				anim.ReplaceAnim(prefix + dir);
			else
				anim.PlayRepeating(prefix + dir);

			return true;
		}

		bool inAttack = false;
		bool IsProne(Actor self)
		{
			var takeCover = self.traits.GetOrDefault<TakeCover>();
			return takeCover != null && takeCover.IsProne;
		}

		public void Attacking(Actor self)
		{
			var dir = Util.QuantizeFacing(self.traits.Get<Unit>().Facing, 8);
			inAttack = true;

			var prefix = IsProne(self) ? "prone-shoot-" : "shoot-";

			anim.PlayThen(prefix + dir, () => inAttack = false);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (inAttack) return;
			if (ChooseMoveAnim(self)) return;

			/* todo: idle anims, etc */

			var dir = Util.QuantizeFacing(self.traits.Get<Unit>().Facing, 8);

			if (IsProne(self))
				anim.PlayFetchIndex("crawl-" + dir, () => 0);			/* what a hack. */
			else
				anim.PlayFacing("stand",
					() => self.traits.Get<Unit>().Facing);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				Sound.PlayVoice("Die", self);
				Game.world.AddFrameEndTask(w => w.Add(new Corpse(self, e.Warhead.InfDeath)));
			}
		}
	}
}
