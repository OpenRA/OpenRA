#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		public readonly string PilotActor = "E1";
		public readonly int SuccessRate = 50;
		public readonly string ChuteSound = "chute1.aud";
	}

	public class EjectOnDeath : INotifyDamage
	{
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead())
			{
				var a = self;
				var info = self.Info.Traits.Get<EjectOnDeathInfo>();
				var pilot = a.World.CreateActor(false, info.PilotActor.ToLowerInvariant(), new TypeDictionary { new OwnerInit(a.Owner) });
				var r = self.World.SharedRandom.Next(1, 100);
				var aircraft = a.Trait<IMove>();

				if (IsSuitableCell(pilot, a.Location) && r > 100 - info.SuccessRate && aircraft.Altitude > 10)
				{
					var rs = pilot.Trait<RenderSimple>();
					

					a.World.AddFrameEndTask(w => w.Add(
							new Parachute(pilot.Owner, rs.anim.Name,
								Util.CenterOfCell(Util.CellContaining(a.CenterLocation)),
								aircraft.Altitude, pilot)));

					Sound.Play(info.ChuteSound, a.CenterLocation);
				}
				else
				{
					pilot.Destroy();
				}
			}
		}

		bool IsSuitableCell(Actor actorToDrop, int2 p)
		{
			return actorToDrop.Trait<ITeleportable>().CanEnterCell(p);
		}
	}
}