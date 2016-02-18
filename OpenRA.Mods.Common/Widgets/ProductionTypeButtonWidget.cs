#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionTypeButtonWidget : ButtonWidget
	{
		public readonly string ProductionGroup;
		public readonly string HotkeyName;

		[ObjectCreator.UseCtor]
		public ProductionTypeButtonWidget(Ruleset modRules)
			: base(modRules) { }

		protected ProductionTypeButtonWidget(ProductionTypeButtonWidget other)
			: base(other)
		{
			ProductionGroup = other.ProductionGroup;
		}
	}
}
