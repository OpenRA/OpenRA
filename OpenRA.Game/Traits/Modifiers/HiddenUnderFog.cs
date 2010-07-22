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

namespace OpenRA.Traits
{
	class HiddenUnderFogInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new HiddenUnderFog(init.self); }
	}

	class HiddenUnderFog : IRenderModifier, IRadarVisibilityModifier
	{
		Shroud shroud;

		public HiddenUnderFog(Actor self)
		{
			shroud = self.World.WorldActor.traits.Get<Shroud>();
		}

		public bool IsVisible(Actor self)
		{
			return self.World.LocalPlayer == null
				|| self.Owner == self.World.LocalPlayer
				|| self.World.LocalPlayer.Shroud.Disabled
				|| shroud.visibleCells[self.Location.X, self.Location.Y] > 0;
		}
		
		public bool VisibleOnRadar(Actor self)
		{
			return IsVisible(self);
		}

		static Renderable[] Nothing = { };
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return IsVisible(self) ? r : Nothing;
		}
	}
}
