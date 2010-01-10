using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits.Activities
{
	class HeliReturn : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;

		static Actor ChooseHelipad(Actor self)
		{
			return Game.world.Actors.FirstOrDefault(
				a => a.Info == Rules.NewUnitInfo["HPAD"] &&
					a.Owner == self.Owner &&
					!Reservable.IsReserved(a));
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var dest = ChooseHelipad(self);

			if (dest == null)
				return Util.SequenceActivities(
					new Turn(self.LegacyInfo.InitialFacing), 
					new HeliLand(true),
					NextActivity);

			var res = dest.traits.GetOrDefault<Reservable>();
			if (res != null)
				self.traits.Get<Helicopter>().reservation = res.Reserve(self);

			var offset = (dest.LegacyInfo as LegacyBuildingInfo).SpawnOffset;
			var offsetVec = offset != null ? new float2(offset[0], offset[1]) : float2.Zero;

			return Util.SequenceActivities(
				new HeliFly(dest.CenterLocation + offsetVec),
				new Turn(self.LegacyInfo.InitialFacing),
				new HeliLand(false),
				new Rearm(),
				NextActivity);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
