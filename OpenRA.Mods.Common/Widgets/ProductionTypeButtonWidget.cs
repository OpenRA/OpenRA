#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionTypeButtonWidget : WorldButtonWidget
	{
		public readonly string ProductionGroup;

		[ObjectCreator.UseCtor]
		public ProductionTypeButtonWidget(ModData modData, World world)
			: base(modData, world) { }

		protected ProductionTypeButtonWidget(ProductionTypeButtonWidget other)
			: base(other)
		{
			ProductionGroup = other.ProductionGroup;
		}
	}
}
