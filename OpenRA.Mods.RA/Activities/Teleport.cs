#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA.Activities
{
	public class Teleport : Activity
	{
		CPos destination;
		bool killCargo;
		Actor chronosphere;
		string sound;

		public Teleport(Actor chronosphere, CPos destination, bool killCargo, string sound)
		{
			this.chronosphere = chronosphere;
			this.destination = destination;
			this.killCargo = killCargo;
			this.sound = sound;
		}

		public override Activity Tick(Actor self)
		{
			Sound.Play(sound, self.CenterPosition);
			Sound.Play(sound, destination.CenterPosition);

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			if (killCargo && self.HasTrait<Cargo>())
			{
				var cargo = self.Trait<Cargo>();
				if (chronosphere != null)
				{
					while (!cargo.IsEmpty(self))
					{
						var a = cargo.Unload(self);
						// Kill all the units that are unloaded into the void
						// Kill() handles kill and death statistics
						a.Kill(chronosphere);
					}
				}
			}

			// Trigger screen desaturate effect
			foreach (var a in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
				a.Trait.Enable();

			if (chronosphere != null && !chronosphere.Destroyed && chronosphere.HasTrait<RenderBuilding>())
				chronosphere.Trait<RenderBuilding>().PlayCustomAnim(chronosphere, "active");

			return NextActivity;
		}
	}

	public class SimpleTeleport : Activity
	{
		CPos destination;

		public SimpleTeleport(CPos destination) { this.destination = destination; }

		public override Activity Tick(Actor self)
		{
			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;
			return NextActivity;
		}
	}
}
