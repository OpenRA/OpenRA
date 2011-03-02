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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ProductionBarInfo : TraitInfo<ProductionBar> { }

	class ProductionBar : ISelectionBar
	{
		public float GetValue() { return .5f; }

		public Color GetColor() { return Color.CadetBlue; }
	}
}
