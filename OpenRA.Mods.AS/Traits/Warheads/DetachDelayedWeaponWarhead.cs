using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Warheads
{
	public class DetachDelayedWeaponWarhead : WarheadAS
	{
		[Desc("Types of attachables that it can detach, as long as the type also exists in the Attachable Type: trait.")]
		public readonly HashSet<string> AttachableTypes = new HashSet<string> { "bomb" };
		
		[Desc("Range of targets to be attached.")]
		public readonly WDist Range = new WDist(1024);

		[Desc("Defines how many objects can be detached per impact.")]
		public readonly int DetachLimit = 1;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var pos = target.CenterPosition;

			if (!IsValidImpact(pos, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsInCircle(pos, Range + VictimScanRadius);
			foreach (var actor in availableActors)
			{
				if (!IsValidAgainst(actor, firedBy))
					continue;

				if (actor.IsDead)
					continue;

				var attachables = actor.TraitsImplementing<DelayedWeaponAttachable>();
				var triggers = attachables.Where(a => AttachableTypes.Any(at => at == a.Info.Type)).SelectMany(a => a.Container);
				triggers.OrderBy(t => t.RemainingTime).Take(DetachLimit).ToList().ForEach(t => t.Deactivate());
			}
		}
	}
}
