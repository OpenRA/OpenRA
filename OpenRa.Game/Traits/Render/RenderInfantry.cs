using OpenRa.Effects;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	public class RenderInfantryInfo : RenderSimpleInfo
	{
		public override object Create(Actor self) { return new RenderInfantry(self); }
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamage
	{
		public RenderInfantry(Actor self)
			: base(self, () => self.traits.Get<Unit>().Facing)
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

			var seq = IsProne(self) ? "crawl" : "run";

			if (anim.CurrentSequence.Name != seq)
				anim.PlayRepeating(seq);

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
			inAttack = true;

			var seq = IsProne(self) ? "prone-shoot" : "shoot";

			if (anim.HasSequence(seq))
				anim.PlayThen(seq, () => inAttack = false);
			else if (anim.HasSequence("heal"))
				anim.PlayThen("heal", () => inAttack = false);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (inAttack) return;
			if (ChooseMoveAnim(self)) return;

			/* todo: idle anims, etc */

			if (IsProne(self))
				anim.PlayFetchIndex("crawl", () => 0);			/* what a hack. */
			else
				anim.PlayFacing("stand",
					() => self.traits.Get<Unit>().Facing);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				Sound.PlayVoice("Die", self);
				self.World.AddFrameEndTask(w => w.Add(new Corpse(self, e.Warhead.InfDeath)));
			}
		}
	}
}
