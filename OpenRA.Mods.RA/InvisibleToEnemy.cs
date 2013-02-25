#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class InvisibleToEnemyInfo : TraitInfo<InvisibleToEnemy> { }

	class InvisibleToEnemy : IRenderModifier, IVisibilityModifier, IRadarColorModifier
	{
		public bool IsVisible(Shroud s, Actor self)
		{
			return self.World.LocalPlayer == null ||
				self.Owner.Stances[self.World.LocalPlayer] == Stance.Ally;
		}

		public Color RadarColorOverride(Actor self)
		{
			// todo: why is making this half-opaque conflated with hiding the actor from non-allies?
			return Color.FromArgb(128, self.Owner.ColorRamp.GetColor(0));
		}

		static readonly Renderable[] Nothing = { };

		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			return IsVisible(self.Owner.Shroud, self) ? r : Nothing;
		}
	}
}
