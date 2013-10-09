#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class EjectOnDeathInfo : TraitInfo<EjectOnDeath>
	{
		[ActorReference]
		public readonly string PilotActor = "MEDI"; // E1
		public readonly int SuccessRate = 100;		//	50
		public readonly string ChuteSound = "chute1.aud";
		public readonly bool PilotEjectInAir = true;
		public readonly bool PilotEjectOnGround = true;
		//public readonly WRange MinimumEjectHeight = new WRange(427);
	}

	public class EjectOnDeath : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			var info = self.Info.Traits.Get<EjectOnDeathInfo>();

			var pilot = self.World.CreateActor
			            (
				            false, info.PilotActor.ToLowerInvariant(),
							new TypeDictionary
							{
								new OwnerInit(self.Owner),
								new LocationInit(self.Location)
							}
				         );

			var r = self.World.SharedRandom.Next(1, 100);
			var cp = self.CenterPosition;

			if (IsSuitableCell(pilot, self.Location)
			    && r > 100 - info.SuccessRate
			    && self.Owner.WinState != WinState.Lost)
			{
				//Game.Debug("Suitable. Success Rate: " + 100 - info.SuccessRate + ". Not Lost.");
				if (cp.Z > 0 && info.PilotEjectInAir == true)
				{
					Game.Debug("Z > 0, EjectInAir: " + info.PilotEjectInAir.ToString());
					self.World.AddFrameEndTask(w => w.Add(new Parachute(pilot, cp)));
					Sound.Play(info.ChuteSound, cp);
				}
				else if (cp.Z == 0 && info.PilotEjectOnGround == true)
				{
					Game.Debug("Z == 0, EjectOnGround: " + info.PilotEjectOnGround.ToString());
					self.World.AddFrameEndTask(w => w.Add(pilot));
				}
				else
				{
					Game.Debug("Something went wrong.");
				}
			}
			else
			{
				Game.Debug("pilot.Destroy()");
				Game.Debug("Suitable: " + IsSuitableCell(pilot, self.Location));    //  this returns false
				Game.Debug("SuccessRate: 100 - " + info.SuccessRate);
				pilot.Destroy();
			}
		}

		bool IsSuitableCell(Actor actorToDrop, CPos p)
		{
			return actorToDrop.Trait<IPositionable>().CanEnterCell(p);
		}
	}
}
