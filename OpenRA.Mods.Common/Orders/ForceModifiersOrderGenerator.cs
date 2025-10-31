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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.Orders
{
	public class ForceModifiersOrderGenerator : UnitOrderGenerator
	{
		public readonly Modifiers Modifiers;
		readonly bool cancelOnFirstUse;

		public ForceModifiersOrderGenerator(Modifiers modifiers, bool cancelOnFirstUse)
		{
			Modifiers = modifiers;
			this.cancelOnFirstUse = cancelOnFirstUse;
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Modifiers |= Modifiers;

			if (cancelOnFirstUse)
				world.CancelInputMode();

			return base.Order(world, cell, worldPixel, mi);
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Modifiers |= Modifiers;
			return base.GetCursor(world, cell, worldPixel, mi);
		}
	}
}
