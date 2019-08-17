#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
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

		public override object Create(ActorInitializer init) { return new EjectOnDeath(init.Self, this); }
	}

	public class EjectOnDeath : ConditionalTrait<EjectOnDeathInfo>, INotifyKilled
	{
		public EjectOnDeath(Actor self, EjectOnDeathInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || self.Owner.WinState == WinState.Lost || !self.World.Map.Contains(self.Location))
				return;

			var r = self.World.SharedRandom.Next(1, 100);

			if (r <= 100 - Info.SuccessRate)
				return;

			var cp = self.CenterPosition;
			var inAir = !self.IsAtGroundLevel();
			if ((inAir && !Info.EjectInAir) || (!inAir && !Info.EjectOnGround))
				return;

			var pilot = self.World.CreateActor(false, Info.PilotActor.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner), new LocationInit(self.Location) });

			var pilotPositionable = pilot.TraitOrDefault<IPositionable>();
			var pilotCell = self.Location;
			var pilotSubCell = pilotPositionable.GetAvailableSubCell(pilotCell);
			if (pilotSubCell == SubCell.Invalid)
			{
				if (!Info.AllowUnsuitableCell)
				{
					pilot.Dispose();
					return;
				}

				pilotSubCell = SubCell.Any;
			}

			if (inAir)
			{
				self.World.AddFrameEndTask(w =>
				{
					pilotPositionable.SetPosition(pilot, pilotCell, pilotSubCell);
					var dropPosition = pilot.CenterPosition + new WVec(0, 0, self.CenterPosition.Z - pilot.CenterPosition.Z);
					pilotPositionable.SetVisualPosition(pilot, dropPosition);

					w.Add(pilot);
				});

				Game.Sound.Play(SoundType.World, Info.ChuteSound, cp);
			}
			else
			{
				self.World.AddFrameEndTask(w =>
				{
					w.Add(pilot);
					pilotPositionable.SetPosition(pilot, pilotCell, pilotSubCell);

					var pilotMobile = pilot.TraitOrDefault<Mobile>();
					if (pilotMobile != null)
						pilotMobile.Nudge(pilot, pilot, true);
				});
			}
		}
	}
}
