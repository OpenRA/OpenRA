#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class UnloadCargo : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;

		int2? ChooseExitTile(Actor self, Actor cargo)
		{
			// is anyone still hogging this tile?
			if (self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(self.Location).Count() > 1)
				return null;
			
			var mobile = cargo.traits.Get<Mobile>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if ((i != 0 || j != 0) && 
						mobile.CanEnterCell(self.Location + new int2(i, j)))
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

			var exitTile = ChooseExitTile(self, cargo.Peek(self));
			if (exitTile == null) 
				return this;

			var actor = cargo.Unload(self);

			self.World.AddFrameEndTask(w =>
			{
				w.Add(actor);
				actor.traits.WithInterface<IMove>().FirstOrDefault().SetPosition(actor, self.Location);
				actor.CancelActivity();
				actor.QueueActivity(new Move(exitTile.Value, 0));
			});

			return this;
		}

		public void Cancel(Actor self) { NextActivity = null; isCanceled = true; }
	}
}
