#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Eject a ground soldier or a paratrooper while in the air.")]
	public class EjectOnDeathInfo : TraitInfo<EjectOnDeath>
	{
		[ActorReference]
		public readonly string PilotActor = "E1";
		public readonly int SuccessRate = 50;
		public readonly string ChuteSound = "chute1.aud";
		public readonly bool EjectInAir = false;
		public readonly bool EjectOnGround = false;

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowUnsuitableCell = false;
	}

	public class EjectOnDeath : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			if (self.Owner.WinState == WinState.Lost)
				return;

			var r = self.World.SharedRandom.Next(1, 100);
			var info = self.Info.Traits.Get<EjectOnDeathInfo>();

			if (r <= 100 - info.SuccessRate)
				return;

			var cp = self.CenterPosition;
			if ((cp.Z > 0 && !info.EjectInAir) || (cp.Z == 0 && !info.EjectOnGround))
				return;

			var pilot = self.World.CreateActor(false, info.PilotActor.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner), new LocationInit(self.Location) });


			if (info.AllowUnsuitableCell || IsSuitableCell(self, pilot))
			{
				if (cp.Z > 0)
				{
					self.World.AddFrameEndTask(w => w.Add(new Parachute(pilot, cp)));
					Sound.Play(info.ChuteSound, cp);
				}
				else
				{
					self.World.AddFrameEndTask(w => w.Add(pilot));
					var pilotMobile = pilot.TraitOrDefault<Mobile>();
					if (pilotMobile != null)
						pilotMobile.Nudge(pilot, pilot, true);
				}
			}
			else
				pilot.Destroy();
		}

		static bool IsSuitableCell(Actor self, Actor actorToDrop)
		{
			return actorToDrop.Trait<IPositionable>().CanEnterCell(self.Location, self, true);
		}
	}
}