#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ParadropCargo : Activity
	{
		readonly bool unloadAll;

		public ParadropCargo(bool unloadAll)
		{
			this.unloadAll = unloadAll;
		}

		public override Activity Tick(Actor self)
		{
			var cargo = self.Trait<Cargo>();
			var notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();

			cargo.Unloading = false;
			if (IsCanceled || cargo.IsEmpty(self))
				return NextActivity;

			foreach (var inu in notifiers)
				inu.Unloading(self);

			var actor = cargo.Peek(self);
			var spawn = self.CenterPosition;

			cargo.Unload(self);
			self.World.AddFrameEndTask(w =>
			{
				if (actor.Disposed)
					return;

				var pos = actor.Trait<IPositionable>();

				pos.SetVisualPosition(actor, spawn);

				actor.CancelActivity();
				actor.QueueActivity(new Parachute(actor, spawn));
				w.Add(actor);
			});
			Game.Sound.Play(SoundType.World, self.Info.TraitInfo<ParaDropInfo>().ChuteSound, spawn);

			if (!unloadAll || cargo.IsEmpty(self))
				return NextActivity;

			cargo.Unloading = true;
			return this;
		}
	}
}
