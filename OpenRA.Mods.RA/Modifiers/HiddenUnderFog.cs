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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HiddenUnderFogInfo : TraitInfo<HiddenUnderFog> {}

	class HiddenUnderFog : IRenderModifier, IVisibilityModifier
	{
		public bool IsVisible(Actor self)
		{
			return Shroud.GetVisOrigins(self).Any(o => self.World.LocalShroud.IsVisible(o));
		}

		static Renderable[] Nothing = { };
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return IsVisible(self) ? r : Nothing;
		}
	}
}
