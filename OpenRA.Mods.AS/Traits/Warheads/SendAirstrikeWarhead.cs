#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

// TODO: Mix this with Spawner logics when those arrive for the airstrike to grant experience to the initiator.
namespace OpenRA.Mods.AS.Warheads
{
	public enum AirstrikeTarget { Target, Position }

	[Desc("This warhead sends an airstrike.")]
	public class SendAirstrikeWarhead : WarheadAS
	{
		[Desc("The mode the airstrike should behave. Available options are Target (where the plane attacks the weapon's target) and Position.")]
		public readonly AirstrikeTarget Mode = AirstrikeTarget.Target;

		[Desc("Should the aircraft fly in from a random edge of the map or use the firer's facing?")]
		public readonly bool RandomizeAircraftFacing = false;

		[ActorReference(typeof(AircraftInfo)), FieldLoader.Require]
		public readonly string UnitType = null;
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy) || firedBy.IsDead)
				return;

			var attackFacing = RandomizeAircraftFacing || !firedBy.Info.HasTraitInfo<IFacingInfo>()
				? 256 * firedBy.World.SharedRandom.Next(QuantizedFacings) / QuantizedFacings
				: firedBy.Trait<IFacing>().Facing;

			var attackRotation = WRot.FromFacing(attackFacing);

			var altitude = new WVec(0, 0, firedBy.World.Map.Rules.Actors[UnitType.ToLowerInvariant()].TraitInfo<AircraftInfo>().CruiseAltitude.Length);
			var delta = new WVec(0, -1024, 0).Rotate(attackRotation);

			var startPos = target.CenterPosition + altitude - (firedBy.World.Map.DistanceToEdge(target.CenterPosition, -delta) + Cordon).Length * delta / 1024;

			firedBy.World.AddFrameEndTask(w =>
			{
				for (var i = -SquadSize / 2; i <= SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);

					var a = w.CreateActor(UnitType, new TypeDictionary
					{
						new CenterPositionInit(startPos + spawnOffset),
						new OwnerInit(firedBy.Owner),
						new FacingInit(attackFacing),
					});

					var plane = !a.Trait<Aircraft>().Info.CanHover;

					if (plane)
					{
						if (Mode == AirstrikeTarget.Target)
							a.QueueActivity(new FlyAttack(a, target));
						else
							a.QueueActivity(new FlyAttack(a, Target.FromPos(target.CenterPosition + spawnOffset)));

						a.QueueActivity(new FlyOffMap(a));
					}
					else
					{
						if (Mode == AirstrikeTarget.Target)
							a.QueueActivity(new HeliAttack(a, target));
						else
							a.QueueActivity(new HeliAttack(a, Target.FromPos(target.CenterPosition + spawnOffset)));

						var finishPos = target.CenterPosition + altitude + (firedBy.World.Map.DistanceToEdge(target.CenterPosition, delta) + Cordon).Length * delta / 1024;
						a.QueueActivity(new HeliFly(a, Target.FromPos(finishPos + spawnOffset)));
					}

					a.QueueActivity(new RemoveSelf());
				};
			});
		}
	}
}
