using OpenRa.Effects;

namespace OpenRa.Traits
{
	public class RenderInfantryInfo : RenderSimpleInfo
	{
		public override object Create(Actor self) { return new RenderInfantry(self); }
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamage
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

			if (anim.HasSequence(prefix + dir))
				anim.PlayThen(prefix + dir, () => inAttack = false);
			else if (anim.HasSequence("heal"))
				anim.PlayThen("heal", () => inAttack = false);
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
