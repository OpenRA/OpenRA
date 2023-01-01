#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Eject a ground soldier or a paratrooper while in the air.")]
	public class EjectOnDeathInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[Desc("Name of the unit to eject. This actor type needs to have the Parachutable trait defined.")]
		public readonly string PilotActor = "E1";

		[Desc("Probability that the aircraft's pilot gets ejected once the aircraft is destroyed.")]
		public readonly int SuccessRate = 50;

		[Desc("Sound to play when ejecting the pilot from the aircraft.")]
		public readonly string ChuteSound = null;

		[Desc("Can a destroyed aircraft eject its pilot while it has not yet fallen to ground level?")]
		public readonly bool EjectInAir = false;

		[Desc("Can a destroyed aircraft eject its pilot when it falls to ground level?")]
		public readonly bool EjectOnGround = false;

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowUnsuitableCell = false;

		public override object Create(ActorInitializer init) { return new EjectOnDeath(this); }
	}

	public class EjectOnDeath : ConditionalTrait<EjectOnDeathInfo>, INotifyKilled
	{
		public EjectOnDeath(EjectOnDeathInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || self.Owner.WinState == WinState.Lost || !self.World.Map.Contains(self.Location))
				return;

			if (self.World.SharedRandom.Next(100) >= Info.SuccessRate)
				return;

			var cp = self.CenterPosition;
			var inAir = !self.IsAtGroundLevel();
			if ((inAir && !Info.EjectInAir) || (!inAir && !Info.EjectOnGround))
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (!Info.AllowUnsuitableCell)
				{
					var pilotInfo = self.World.Map.Rules.Actors[Info.PilotActor.ToLowerInvariant()];
					var pilotPositionable = pilotInfo.TraitInfo<IPositionableInfo>();
					if (!pilotPositionable.CanEnterCell(self.World, null, self.Location))
						return;
				}

				var td = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new LocationInit(self.Location),
				};

				// If airborne, offset the spawn location so the pilot doesn't drop on another infantry's head
				var spawnPos = cp;
				if (inAir)
				{
					var subCell = self.World.ActorMap.FreeSubCell(self.Location);
					if (subCell != SubCell.Invalid)
					{
						td.Add(new SubCellInit(subCell));
						spawnPos = self.World.Map.CenterOfSubCell(self.Location, subCell) + new WVec(0, 0, spawnPos.Z);
					}
				}

				td.Add(new CenterPositionInit(spawnPos));

				var pilot = self.World.CreateActor(true, Info.PilotActor.ToLowerInvariant(), td);

				if (!inAir)
					pilot.TraitOrDefault<Mobile>()?.Nudge(pilot);
				else
					Game.Sound.Play(SoundType.World, Info.ChuteSound, cp);
			});
		}
	}
}
