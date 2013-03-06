#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits
{
	public class CreatesShroudInfo : ITraitInfo
	{
		public readonly int Range = 0;
		public object Create(ActorInitializer init) { return new CreatesShroud(this); }
	}

	public class CreatesShroud : ITick, ISync
	{
		CreatesShroudInfo Info;
		[Sync] CPos previousLocation;
		Shroud.ActorVisibility v;

		public CreatesShroud(CreatesShroudInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{
	    	// todo: don't tick all the time.
			if(self.Owner == null) return;

			if (previousLocation != self.Location && v != null) {
				previousLocation = self.Location;

				var shrouds = self.World.ActorsWithTrait<Shroud>().Select(s => s.Actor.Owner.Shroud);
				foreach (var shroud in shrouds) {
					shroud.UnhideActor(self, v, Info.Range);
				}
			}

			if (!self.TraitsImplementing<IDisable>().Any(d => d.Disabled)) {
				var shrouds = self.World.ActorsWithTrait<Shroud>().Select(s => s.Actor.Owner.Shroud);
				foreach (var shroud in shrouds) {
					shroud.HideActor(self, Info.Range);
				}
			}
			else {
				var shrouds = self.World.ActorsWithTrait<Shroud>().Select(s => s.Actor.Owner.Shroud);
				foreach (var shroud in shrouds) {
					shroud.UnhideActor(self, v, Info.Range);
				}
			}

			v = new Shroud.ActorVisibility {
				vis = Shroud.GetVisOrigins(self).ToArray()
			};
		}
	}
}