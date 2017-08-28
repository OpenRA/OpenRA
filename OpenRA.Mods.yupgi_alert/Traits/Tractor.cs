using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("Can this actor lift the actor that has Tractable trait and move it next to self by force?")]
	public class TractorInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new Tractor(init.Self, this); }
	}

	public class Tractor : ConditionalTrait<TractorInfo>, INotifyAttack
	{
		// readonly Actor self;
		Actor target;
		Tractable targetTractable;

		public Tractor(Actor self, TractorInfo info) : base(info)
		{
		}

		/// <summary>
		/// Start tracting victim
		/// </summary>
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (target.Actor == null)
				return;

			this.target = target.Actor;
			targetTractable = this.target.TraitOrDefault<Tractable>();
			if (targetTractable == null)
				return;

			targetTractable.Tract(this.target, self);
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }
	}
}
