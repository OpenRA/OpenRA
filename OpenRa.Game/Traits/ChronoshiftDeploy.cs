using OpenRa.Game.GameRules;
using System.Drawing;
namespace OpenRa.Game.Traits
{
    class ChronoshiftDeploy : IOrder, ISpeedModifier, ITick, IPips
    {
        public ChronoshiftDeploy(Actor self) { }
        bool chronoshiftActive = false; // Is the chronoshift engine active?
        const int chargeTime = 100; // How many frames between uses?
        int remainingChargeTime = 0;

        public void Tick(Actor self)
        {
            if (remainingChargeTime > 0)
                remainingChargeTime--;
        }

        public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
        {
            if (mi.Button == MouseButton.Left) return null;

            if (chronoshiftActive)
                return Order.UsePortableChronoshift(self, xy);

            else if (xy == self.Location && remainingChargeTime <= 0)
                return Order.ActivatePortableChronoshift(self);
 
            return null;
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == "ActivatePortableChronoshift" && remainingChargeTime <= 0)
            {
                chronoshiftActive = true;
                self.CancelActivity();
            }

            if (order.OrderString == "UsePortableChronoshift" && CanEnterCell(order.TargetLocation, self))
            {
           		//self.QueueActivity(new Activities.Teleport(order.TargetLocation));
                Sound.Play("chrotnk1.aud");
                chronoshiftActive = false;
                remainingChargeTime = chargeTime;
            }
        }

        static bool CanEnterCell(int2 c, Actor self)
        {
            if (!Game.BuildingInfluence.CanMoveHere(c)) return false;
            var u = Game.UnitInfluence.GetUnitAt(c);
            return (u == null || u == self);
        }

        public float GetSpeedModifier()
        {
            return chronoshiftActive ? 0f : 1f;
        }

        public Color GetBorderColor() { return Color.Black; }
        public int GetPipCount() { return 5; }
        public Color GetColorForPip(int index)
        {
            // TODO: Check how many pips to display
            if ((1 - remainingChargeTime*1.0f / chargeTime) * GetPipCount() < index + 1)
                return Color.Transparent;

            switch (index)
            {
                case 0:
                case 1:
                    return Color.Red;
                case 2:
                case 3:
                    return Color.Yellow;
                case 4:
                    return Color.LimeGreen;
            }

            return Color.Transparent;
        }
    }
}
