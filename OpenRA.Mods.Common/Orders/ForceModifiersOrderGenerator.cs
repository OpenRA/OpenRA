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
		protected override MouseActionType ActionType => MouseActionType.ConfirmOrder;
		public readonly Modifiers Modifiers;
		readonly bool cancelOnFirstUse;

		public ForceModifiersOrderGenerator(World world, Modifiers modifiers, bool cancelOnFirstUse)
			: base(world)
		{
			Modifiers = modifiers;
			this.cancelOnFirstUse = cancelOnFirstUse;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Modifiers |= Modifiers;
			if ((cancelOnFirstUse && !mi.Modifiers.HasModifier(Modifiers.Shift)) || mi.Button == CancelButton)
				world.CancelInputMode();

			return base.OrderInner(world, cell, worldPixel, mi);
		}

		public override bool ClearSelectionOnLeftClick => false;

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Modifiers |= Modifiers;
			return base.GetCursor(world, cell, worldPixel, mi);
		}
	}
}
