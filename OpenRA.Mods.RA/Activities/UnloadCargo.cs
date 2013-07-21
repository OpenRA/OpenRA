#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class UnloadCargo : Activity
	{
		bool unloadAll;

		public UnloadCargo(bool unloadAll) { this.unloadAll = unloadAll; }
		
		CPos? ChooseExitTile(Actor self, Actor cargo)
		{
			// is anyone still hogging this tile?
			if (self.World.ActorMap.GetUnitsAt(self.Location).Count() > 1)
				return null;

			var mobile = cargo.Trait<Mobile>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if ((i != 0 || j != 0) &&
						mobile.CanEnterCell(self.Location + new CVec(i, j)))
						return self.Location + new CVec(i, j);

			return null;
		}

		CPos? ChooseRallyPoint(Actor self)
		{
			var mobile = self.Trait<Mobile>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if ((i != 0 || j != 0) &&
						mobile.CanEnterCell(self.Location + new CVec(i, j)))
						return self.Location + new CVec(i, j);

			return self.Location;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;

			// if we're a thing that can turn, turn to the
			// right facing for the unload animation
			var facing = self.TraitOrDefault<IFacing>();
			var unloadFacing = self.Info.Traits.Get<CargoInfo>().UnloadFacing;
			if (facing != null && facing.Facing != unloadFacing)
				return Util.SequenceActivities( new Turn(unloadFacing), this );

			// TODO: handle the BS of open/close sequences, which are inconsistent,
			//		for reasons that probably make good sense to the westwood guys.

			var cargo = self.Trait<Cargo>();
			if (cargo.IsEmpty(self))
				return NextActivity;

			var ru = self.TraitOrDefault<RenderUnit>();
			if (ru != null)
				ru.PlayCustomAnimation(self, "unload", null);

			var exitTile = ChooseExitTile(self, cargo.Peek(self));
			if (exitTile == null)
				return this;

			var actor = cargo.Unload(self);
			var exit = exitTile.Value.CenterPosition;
			var current = self.Location.CenterPosition;

			self.World.AddFrameEndTask(w =>
			{
				if (actor.Destroyed)
					return;

				var mobile = actor.Trait<Mobile>();
				mobile.Facing = Util.GetFacing(exit - current, mobile.Facing );
				mobile.SetPosition(actor, exitTile.Value);
				mobile.SetVisualPosition(actor, current);
				var speed = mobile.MovementSpeedForCell(actor, exitTile.Value);
				var length = speed > 0 ? (exit - current).Length / speed : 0;

				w.Add(actor);
				actor.CancelActivity();
				actor.QueueActivity(new Drag(current, exit, length));
				actor.QueueActivity(mobile.MoveTo(exitTile.Value, 0));

				var rallyPoint = ChooseRallyPoint(actor).Value;
				actor.QueueActivity(mobile.MoveTo(rallyPoint, 0));
				actor.SetTargetLine(Target.FromCell(rallyPoint), Color.Green, false);
			});

			return unloadAll ? this : NextActivity;
		}
	}
}
