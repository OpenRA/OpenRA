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
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class InvisibleToOthersInfo : TraitInfo<InvisibleToOthers> { }

	class InvisibleToOthers : IRenderModifier, IVisibilityModifier, IRadarColorModifier
	{
		public bool IsVisible(Actor self, Player byPlayer)
		{
			return self.Owner == byPlayer;
		}
		
		public Color RadarColorOverride(Actor self)
		{
			return Color.FromArgb(128, self.Owner.Color);
		}
		
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return self.World.LocalPlayer == self.Owner
				? r : new Renderable[] { };
		}
	}
}
