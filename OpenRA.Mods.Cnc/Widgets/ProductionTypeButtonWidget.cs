#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class ProductionTypeButtonWidget : ButtonWidget
	{
		public readonly string ProductionGroup;

		[ObjectCreator.UseCtor]
		public ProductionTypeButtonWidget(MapRuleset rules)
			: base(rules) { }

		protected ProductionTypeButtonWidget(ProductionTypeButtonWidget other)
			: base(other)
		{
			ProductionGroup = other.ProductionGroup;
		}
	}
}
