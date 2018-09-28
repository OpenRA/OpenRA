#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
    public class SplitExplodes : UpdateRule
    {
        public override string Name { get { return "Remove Explodes EmptyWeapon parameters"; } }
        public override string Description
        {
            get
            {
                return "Explodes.EmptyWeapon has been removed as it became obsolete by introducing conditions.\n" +
                       "Using conditions allows to specify more precisely when and which Explodes should be used.";
            }
        }

        readonly List<Tuple<string, string>> emptyWeaponLocations = new List<Tuple<string, string>>();

        public override IEnumerable<string> AfterUpdate(ModData modData)
        {
            var message1 = "EmptyWeapon has been removed from Explodes.\n"
                           + "Instead, use a second Explodes, and select the correct one using the RequiresCondition property.\n"
                           + "If you were using the LoadedChance property, grant a condition by using GrantRandomCondition.\n"
                           + "The following actors have been updated and might need manual adjustments:\n"
                           + UpdateUtils.FormatMessageList(emptyWeaponLocations.Select(n => n.Item1 + " (" + n.Item2 + ")"));

            if (emptyWeaponLocations.Any())
                yield return message1;

            emptyWeaponLocations.Clear();
        }

        public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
        {
            foreach (var explodes in actorNode.ChildrenMatching("Explodes"))
            {
                var weapon = explodes.LastChildMatching("Weapon");
                var emptyWeapon = explodes.LastChildMatching("EmptyWeapon");
                var loadedChance = explodes.LastChildMatching("LoadedChance");

                if (weapon == null && emptyWeapon == null)
                    explodes.AddNode("Weapon", "UnitExplode");
                else if (weapon == null)
                    emptyWeapon.Key = "Weapon";
                else if (emptyWeapon != null)
                {
                    emptyWeaponLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
                    explodes.RemoveNode(emptyWeapon);
                }

                if (loadedChance != null)
                    explodes.RemoveNode(loadedChance);
            }

            yield break;
        }
    }
}
