#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
        /* tag trait for "ProductionBuildings": mcv/fact, barr/tent, weap, spen/syrd, hpad/afld */
        public class ProductionBuildingInfo : TraitInfo<ProductionBuilding>
        {
                public string BuildingType;
        }

        public class ProductionBuilding { }
}
