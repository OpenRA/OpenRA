using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
    public class OrdererInfo : ITraitInfo
    {
        [Desc("Takes a list of player names which should be affected. Default is everyone.")]
        public readonly string[] Affects = null;
        [Desc("Takes a list of integers. This is where the units will target. ex: Location: 25,25. This will make them target that location. If you enter multiple coordinates it will randomize between them. Example: Location: 25,25,50,50.")]
        public readonly int[] Location = null;
        [Desc("How often the trait should check for units to give orders to. Default is every two seconds.")]
        public readonly int Delay = 50;
        [Desc("What order to give the units. Default is AttackMove. It accepts AttackMove or Move")]
        public readonly string Order = "AttackMove";
        public object Create(ActorInitializer init) { return new Orderer(init.self, this); }
    }

    public class Orderer : ITick
    {
        public OrdererInfo Info;
        List<CPos> locations;
        World world;
        public Orderer(Actor self, OrdererInfo info)
        {
            Info = info;
            locations = new List<CPos>();
            if (Info.Location != null && Info.Location.Length % 2 == 0 && Info.Location.Length > 0)
                for (int i = 0; i + 1 < Info.Location.Length; i+=2)
                    locations.Add(new CPos(Info.Location[i], Info.Location[i + 1]));
        }

        CPos randomLocation()
        {
            if (locations.Count > 1)
                return locations[world.SharedRandom.Next(0, locations.Count() - 1)];
            return locations[0];
        }

        int delay = 0;
        public void Tick(Actor self)
        {
            if (world == null)
                world = self.World;
            if (locations.Count > 0 && delay % Info.Delay == 0)
            {
                var location = randomLocation();
                if (location != CPos.Zero)
                {
                    var actors = self.World.ActorMap.GetUnitsAt(self.Location);
                    foreach (Actor actor in actors)
                    {
                        var isAffected = Info.Affects == null || Info.Affects.Contains(actor.Owner.InternalName);
                        if (isAffected)
                        {
                            if (Info.Order == "AttackMove" && actor.HasTrait<AttackMove>())
                                actor.Trait<AttackMove>().ResolveOrder(actor, new Order("AttackMove", actor, false) { TargetLocation = location });
                            else if (Info.Order == "Move" && actor.HasTrait<Mobile>())
                                actor.Trait<Mobile>().ResolveOrder(actor, new Order("Move", actor, false) { TargetLocation = location });
                        }
                    }
                }
            }
            delay++;
        }
    }
}
