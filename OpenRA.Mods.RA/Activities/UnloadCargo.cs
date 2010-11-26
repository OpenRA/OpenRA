#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Drawing;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	public class UnloadCargo : CancelableActivity
	{
		int2? ChooseExitTile(Actor self, Actor cargo)
		{
			// is anyone still hogging this tile?
			if (self.World.WorldActor.Trait<UnitInfluence>().GetUnitsAt(self.Location).Count() > 1)
				return null;
			
			var mobile = cargo.Trait<Mobile>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
					if ((i != 0 || j != 0) && 
						mobile.CanEnterCell(self.Location + new int2(i, j)))
						return self.Location + new int2(i, j);

			return null;
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;

			// if we're a thing that can turn, turn to the
			// right facing for the unload animation
			var facing = self.TraitOrDefault<IFacing>();
			var unloadFacing = self.Info.Traits.Get<CargoInfo>().UnloadFacing;
			if (facing != null && facing.Facing != unloadFacing)
				return Util.SequenceActivities( new Turn(unloadFacing), this );

			// todo: handle the BS of open/close sequences, which are inconsistent,
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

			self.World.AddFrameEndTask(w =>
			{
				if (actor.Destroyed) return;
				w.Add(actor);

				var mobile = actor.Trait<Mobile>();
				mobile.SetPosition(actor, self.Location);
				actor.CancelActivity();
				actor.QueueActivity(mobile.MoveTo(exitTile.Value, 0));
				actor.SetTargetLine(Target.FromCell(exitTile.Value), Color.Green, false);
			});

			return this;
		}
	}
}
