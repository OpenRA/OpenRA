using System;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Issues repair orders as soon as a building is hit.
    /// </summary>
    internal class HackyAIDamageRepairStrategy : IStrategy, IDamageStrategy
    {
        #region Private Fields
        private bool shouldRepairBuildings;
        private Statistics statistics;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the damage repair strategy
        /// </summary>
        /// <param name="shouldRepairBuildings"></param>
        /// <param name="statistics"></param>
        public HackyAIDamageRepairStrategy(bool shouldRepairBuildings, Statistics statistics)
        {
            this.shouldRepairBuildings = shouldRepairBuildings;
            this.statistics = statistics;
        }
        #endregion

        #region IStrategy Members
        /// <summary>
        /// Very early in each AI operation.
        /// </summary>
        public int OrderDuringTick
        {
            get { return 10; }
        }

        /// <summary>
        /// Regularly do nothing
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="ticks"></param>
        public void Tick(Actor playerActor, Player aiPlayer, int ticks)
        {
        }
        #endregion

        #region IDamageStrategy Members
        /// <summary>
        /// On damage issue a repair
        /// </summary>
        /// <param name="damagedActor"></param>
        /// <param name="e"></param>
        public void Damaged(Actor damagedActor, AttackInfo e)
        {
            if (damagedActor == null)
            {
                throw new ArgumentNullException("damagedActor");
            }
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            if (this.statistics == null)
            {
                throw new InvalidOperationException("HackyAIDamageRepairStrategy.statistics is not set");
            }

            if (shouldRepairBuildings && damagedActor.HasTrait<RepairableBuilding>())
            {
                if (e.DamageState > DamageState.Light && e.PreviousDamageState <= DamageState.Light)
                {
                    Game.Debug("Bot noticed damage {0} {1}->{2}, repairing.", damagedActor, e.PreviousDamageState, e.DamageState);

                    damagedActor.World.IssueOrder(new Order("RepairBuilding", damagedActor.Owner.PlayerActor, false) { TargetActor = damagedActor });
                }
            }

            if (e.Attacker != null && e.Damage > 0)
            {
                statistics.Aggressors[e.Attacker.Owner].ProducedDamage += e.Damage;
            }
        }
        #endregion
    }
}
