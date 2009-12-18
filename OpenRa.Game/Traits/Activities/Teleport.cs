using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits.Activities
{
    class Teleport : IActivity
    {
        public IActivity NextActivity { get; set; }

        int2 destination;

        public Teleport(int2 destination)
        {
            this.destination = destination;
        }

        public IActivity Tick(Actor self)
        {
            var unit = self.traits.Get<Unit>();
            var mobile = self.traits.Get<Mobile>();

            //TODO: Something needs to go here to shift the units position.
            // Everything i have tried has caused a crash in UnitInfluenceMap.

            //Game.world.AddFrameEndTask(_ =>
            //{
                Game.UnitInfluence.Remove(self, mobile);
                //self.Location = this.destination;
                mobile.toCell = this.destination;
                Game.UnitInfluence.Add(self, mobile);

                
            //});

            return null;
        }

        public void Cancel(Actor self){}
       
    }
}
