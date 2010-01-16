using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	class UnloadCargo : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;

		int2? ChooseExitTile(Actor self)
		{
			// is anyone still hogging this tile?
			if (Game.world.UnitInfluence.GetUnitsAt(self.Location).Count() > 1)
				return null;

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if ((i != 0 || j != 0) && 
						Game.IsCellBuildable(self.Location + new int2(i, j), 
							UnitMovementType.Foot))
						return self.Location + new int2(i, j);

			return null;
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;

			// if we're a thing that can turn, turn to the
			// right facing for the unload animation
			var unit = self.traits.GetOrDefault<Unit>();
			var unloadFacing = self.Info.Traits.Get<CargoInfo>().UnloadFacing;
			if (unit != null && unit.Facing != unloadFacing)
				return new Turn(unloadFacing) { NextActivity = this };

			// todo: handle the BS of open/close sequences, which are inconsistent,
			//		for reasons that probably make good sense to the westwood guys.

			var cargo = self.traits.Get<Cargo>();
			if (cargo.IsEmpty(self))
				return NextActivity;

			var ru = self.traits.GetOrDefault<RenderUnit>();
			if (ru != null)
				ru.PlayCustomAnimation(self, "unload", null);

			var exitTile = ChooseExitTile(self);
			if (exitTile == null) 
				return this;

			var actor = cargo.Unload(self);

			Game.world.AddFrameEndTask(w =>
			{
				w.Add(actor);
				actor.traits.Get<Mobile>().TeleportTo(actor, self.Location);
				actor.CancelActivity();
				actor.QueueActivity(new Move(exitTile.Value, 0));
			});

			return this;
		}

		public void Cancel(Actor self) { NextActivity = null; isCanceled = true; }
	}
}
