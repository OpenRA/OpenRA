﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ParaDropInfo : TraitInfo<ParaDrop>
	{
		public readonly int LZRange = 4;
		public readonly string ChuteSound = "chute1.aud";
	}

	public class ParaDrop : ITick
	{
		readonly List<CPos> droppedAt = new List<CPos>();
		CPos lz;

		public void SetLZ(CPos lz)
		{
			this.lz = lz;
			droppedAt.Clear();
		}

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<ParaDropInfo>();
			var r = info.LZRange;

			if ((self.Location - lz).LengthSquared <= r * r && !droppedAt.Contains(self.Location))
			{
				var cargo = self.Trait<Cargo>();
				if (cargo.IsEmpty(self))
					FinishedDropping(self);
				else
				{
					if (!IsSuitableCell(cargo.Peek(self), self.Location))
						return;

					// unload a dude here
					droppedAt.Add(self.Location);

					var a = cargo.Unload(self);

					var aircraft = self.Trait<IMove>();
					self.World.AddFrameEndTask(w => w.Add(
						new Parachute(
							self.Owner,
							Util.CenterOfCell(self.CenterLocation.ToCPos()),
							aircraft.Altitude, a
						)
					));

					Sound.Play(info.ChuteSound, self.CenterLocation);
				}
			}
		}

		bool IsSuitableCell(Actor actorToDrop, CPos p)
		{
			return actorToDrop.Trait<ITeleportable>().CanEnterCell(p);
		}

		void FinishedDropping(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new FlyOffMap());
			self.QueueActivity(new RemoveSelf());
		}
	}
}
