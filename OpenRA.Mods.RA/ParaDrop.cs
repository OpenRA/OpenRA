#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	public class ParaDropInfo : TraitInfo<ParaDrop>
	{
		public readonly int LZRange = 4;
		public readonly string ChuteSound = "chute1.aud";
	}

	public class ParaDrop : ITick
	{
		readonly List<int2> droppedAt = new List<int2>();
		int2 lz;
		Actor flare;

		public void SetLZ( int2 lz, Actor flare )
		{
			this.lz = lz;
			this.flare = flare;
			droppedAt.Clear();
		}

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<ParaDropInfo>();
			var r = info.LZRange;

			if ((self.Location - lz).LengthSquared <= r * r && !droppedAt.Contains(self.Location))
			{
				var cargo = self.traits.Get<Cargo>();
				if (cargo.IsEmpty(self))
					FinishedDropping(self);
				else
				{
					if (!IsSuitableCell(cargo.Peek(self), self.Location))
						return;

					// unload a dude here
					droppedAt.Add(self.Location);

					var a = cargo.Unload(self);
					var rs = a.traits.Get<RenderSimple>();

					var aircraft = self.traits.Get<IMove>();
					self.World.AddFrameEndTask(w => w.Add(
						new Parachute(self.Owner, rs.anim.Name,
							Util.CenterOfCell(Util.CellContaining(self.CenterLocation)),
							aircraft.Altitude, a)));

					Sound.Play(info.ChuteSound, self.CenterLocation);
				}
			}
		}

		bool IsSuitableCell(Actor actorToDrop, int2 p)
		{
			return actorToDrop.traits.Get<ITeleportable>().CanEnterCell(p);
		}

		void FinishedDropping(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new FlyOffMap { Interruptible = false });
			self.QueueActivity(new RemoveSelf());

			if (flare != null)
			{
				flare.CancelActivity();
				flare.QueueActivity(new Wait(300));
				flare.QueueActivity(new RemoveSelf());
			}
		}
	}
}
