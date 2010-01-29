using System.Collections.Generic;
using OpenRa.Mods.RA.Effects;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.RA
{
	class ParaDropInfo : ITraitInfo
	{
		public readonly int LZRange = 4;
		public object Create(Actor self) { return new ParaDrop(); }
	}

	class ParaDrop : ITick
	{
		readonly List<int2> droppedAt = new List<int2>();
		int2 lz;

		public void SetLZ( int2 lz )
		{
			this.lz = lz;
			droppedAt.Clear();
		}

		public void Tick(Actor self)
		{
			var r = self.Info.Traits.Get<ParaDropInfo>().LZRange;

			if ((self.Location - lz).LengthSquared <= r * r && !droppedAt.Contains(self.Location))
			{
				// todo: check is this is a good drop cell.

				// unload a dude here
				droppedAt.Add(self.Location);

				var cargo = self.traits.Get<Cargo>();
				if (cargo.IsEmpty(self))
					FinishedDropping(self);
				else
				{
					var a = cargo.Unload(self);
					var rs = a.traits.Get<RenderSimple>();

					self.World.AddFrameEndTask(w => w.Add(
						new Parachute(self.Owner, rs.anim.Name,
							self.CenterLocation,
							self.traits.Get<Unit>().Altitude, a)));

					Sound.Play("chute1.aud");
				}
			}
		}

		void FinishedDropping(Actor self)
		{
			// this kindof sucks, actually.
			self.CancelActivity();
			self.QueueActivity(new Fly(Util.CenterOfCell(self.World.ChooseRandomEdgeCell())));
			self.QueueActivity(new RemoveSelf());
		}
	}
}
