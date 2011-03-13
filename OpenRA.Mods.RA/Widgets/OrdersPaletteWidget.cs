#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
    class OrdersPaletteWidget : LabelWidget
    {
        readonly World world;

        [ObjectCreator.UseCtor]
        public OrdersPaletteWidget([ObjectCreator.Param] World world)
            : base()
        {
            this.world = world;
            GetText = GetOrderPaletteText;
        }

        string GetOrderPaletteText()
        {
            var possibleOrders = world.Selection.Actors
                .Where(a => !a.Destroyed && a.Owner == a.World.LocalPlayer)
                .SelectMany(a => a.TraitsImplementing<IIssueOrder>())
                .SelectMany(io => io.Orders)
                .Select(ot => ot.OrderID)
                .Distinct();

            return "Orders: \n"
                + string.Join("", possibleOrders.Select(ot => "- {0}\n".F(ot)).ToArray());
        }
    }
}
