using System.Linq;

namespace OpenRa.Game.Traits
{
	class RenderUnitReload : RenderUnit
	{
		public RenderUnitReload(Actor self)
			: base(self) { }

		public override void Tick(Actor self)
		{
			var isAttacking = self.GetCurrentActivity() is Activities.Attack;

			var attack = self.traits.WithInterface<AttackBase>().FirstOrDefault();

			if (attack != null)
				anim.ReplaceAnim((attack.IsReloading() ? "empty-" : "")
					+ (isAttacking ? "aim" : "idle"));
			base.Tick(self);
		}
	}
}
