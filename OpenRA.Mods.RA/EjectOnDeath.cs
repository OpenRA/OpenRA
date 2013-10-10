#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class EjectOnDeathInfo : TraitInfo<EjectOnDeath>
	{
		[ActorReference]
		public readonly string PilotActor = "E1";
		public readonly int SuccessRate = 50;
		public readonly string ChuteSound = "chute1.aud";
		public readonly bool EjectInAir = false;
		public readonly bool EjectOnGround = false;
	}

	public class EjectOnDeath : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			var info = self.Info.Traits.Get<EjectOnDeathInfo>();
			var r = self.World.SharedRandom.Next(1, 100);

			if (r <= 100 - info.SuccessRate || self.Owner.WinState != WinState.Lost)
				return;

			var pilot = self.World.CreateActor(false, info.PilotActor.ToLowerInvariant(),new TypeDictionary{new OwnerInit(self.Owner),new LocationInit(self.Location)});
			var cp = self.CenterPosition;

			if (IsSuitableCell(self, pilot, self.Location)/* && r > 100 - info.SuccessRate && self.Owner.WinState != WinState.Lost*/)
			{
				if (cp.Z > 0 && info.EjectInAir)
				{
					self.World.AddFrameEndTask(w => w.Add(new Parachute(pilot, cp)));
					Sound.Play(info.ChuteSound, cp);
				}
				else if (cp.Z == 0 && info.EjectOnGround)
					self.World.AddFrameEndTask(w => w.Add(pilot));
			}
			else
				pilot.Destroy();
		}

		bool IsSuitableCell(Actor self, Actor actorToDrop, CPos p)
		{
			var info = self.Info.Traits.Get<EjectOnDeathInfo>();
			var cp = self.CenterPosition;

			if (cp.Z == 0 && info.EjectOnGround)
			{
				if (self.World.ActorMap.GetUnitsAt(self.Location).Count() >= 1)
					return false;
				else
					return true;
			}
			if (cp.Z > 0 && info.EjectInAir)
				return actorToDrop.Trait<IPositionable>().CanEnterCell(p);

			return false;
		}
	}
}
