#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [TraitLocation(SystemActors.World | SystemActors.World)]
    [Desc("Allows to group MapOptions in categories.")]
    public class LobbyOptionCategoryInfo : TraitInfo<LobbyOptionCategory>
    {
        [Desc("Category id.")]
        [FieldLoader.Require]
        public readonly string Category = null;

        [Desc("Category title.")]
        public readonly string Title = "";

        [Desc("Category sorting order.")]
        public readonly int DisplayOrder = 0;
    }

    public class LobbyOptionCategory { }
}
