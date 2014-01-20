#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.FileFormats;

namespace OpenRA.Mods.D2k
{
    class SpewsShrapnelInfo : ITraitInfo
    {
        public readonly int Pieces = 3;
        [WeaponReference]
        public string[] Shrapnels = { };
        public readonly bool RandomImages = false;
        public object Create(ActorInitializer init) { return new SpewsShrapnel(this); }
    }

    class SpewsShrapnel : INotifyKilled
    {
        SpewsShrapnelInfo info;

        public SpewsShrapnel (SpewsShrapnelInfo info) 
        {
            this.info = info;
        }

        public void Killed(Actor self, AttackInfo attacker)
        {
            foreach (var arm in info.Shrapnels) {
                var args = new ProjectileArgs
                {
                    Weapon = Rules.Weapons[arm.ToLowerInvariant()],
                    FirepowerModifier = self.TraitsImplementing<IFirepowerModifier>()
                        .Select(a => a.GetFirepowerModifier())
                        .Product(),

                    Source = self.CenterPosition,
                    SourceActor = self,
                };

               var projectile = args.Weapon.Projectile.Create(args);
               if (projectile != null)
                   self.World.AddFrameEndTask(w => w.Add(projectile));

               if (args.Weapon.Report != null && args.Weapon.Report.Any())
                    Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);  
            }
        }
    }
}
