#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	public class UnitOrderViewLogic : ChromeLogic
	{
		UnitOrderViewerWidget view;

		[ObjectCreator.UseCtor]
		public UnitOrderViewLogic(Widget widget, OrderManager orderManager, World world)
		{
			view = widget.Get<UnitOrderViewerWidget>("UNIT_ORDER_VIEWER");
			world.Selection.OnSelectionChanged += view.OnSelectionChange;
		}
	}
}
