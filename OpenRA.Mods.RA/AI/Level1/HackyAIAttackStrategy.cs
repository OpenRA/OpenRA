using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// HackyAI approach to attack. Gather some units until you reach a sqadsize, the attack either nearby units or enemy in general
    /// </summary>
    /// <remarks>
    /// Also keep harvesters to harvest.
    /// </remarks>
    internal class HackyAIAttackStrategy : IStrategy
    {
        #region Private Fields
        private XRandom random = new XRandom();
        #region Setting
        /// <summary>
        /// The number of element in a squad which attacks opponents
        /// </summary>
        private int squadSize;
        #endregion

        private Statistics statistics;

        //hacks etc sigh mess.
        //A bunch of hardcoded lists to keep track of which units are doing what.
        private List<Actor> unitsHangingAroundTheBase = new List<Actor>();
        // all units available for attacking someone
        private List<Actor> attackForce = new List<Actor>();
        //Units that the ai already knows about. Any unit not on this list needs to be given a role.
        private List<Actor> activeUnits = new List<Actor>();
        // the target of the current attack
        private CPos? attackTarget;

        /// <summary>
        /// Remaining ticks till re-assign happens
        /// </summary>
        private int assignRolesTicks = 0;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the attack strategy
        /// </summary>
        /// <param name="squadSize"></param>
        /// <param name="statistics">The shared statistics</param>
        public HackyAIAttackStrategy(int squadSize, Statistics statistics)
        {
            this.squadSize = squadSize;
            this.statistics = statistics;
        }
        #endregion

        #region IStrategy Members
        /// <summary>
        /// In the middle of the AI tick cycle.
        /// </summary>
        public int OrderDuringTick
        {
            get { return 50; }
        }

        /// <summary>
        /// Assign roles to all idle units
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="ticks"></param>
        public void Tick(Actor playerActor, Player aiPlayer, int ticks)
        {
            AssignRolesToIdleUnits(playerActor, aiPlayer);
        }
        #endregion

        #region Helper
        /// <summary>
        /// Assign roles to idle elements in the base camp
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        private void AssignRolesToIdleUnits(Actor playerActor, Player aiPlayer)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (aiPlayer == null)
            {
                throw new ArgumentNullException("aiPlayer");
            }

            //HACK: trim these lists -- we really shouldn'position be hanging onto all this state
            //when it's invalidated so easily, but that's Matthew/Alli's problem.
            activeUnits.RemoveAll(a => a.Destroyed);
            unitsHangingAroundTheBase.RemoveAll(a => a.Destroyed);
            attackForce.RemoveAll(a => a.Destroyed);

            if (--assignRolesTicks < 0)
            {
                assignRolesTicks = Level1AIConstants.AssignRolesInterval;

                // Find idle harvesters and give them orders:
                foreach (Actor activeUnit in activeUnits)
                {
                    Harvester harvesterTrait = activeUnit.TraitOrDefault<Harvester>();

                    if (harvesterTrait == null) continue;

                    if (!activeUnit.IsIdle)
                    {
                        Activity act = activeUnit.GetCurrentActivity();

                        // A Wait activity is technically idle:
                        if (!(act is Activities.Wait) &&
                            (act.NextActivity == null || !(act.NextActivity is Activities.FindResources)))
                        {
                            continue;
                        }
                    }

                    if (!harvesterTrait.IsEmpty)
                    {
                        continue;
                    }

                    // Tell the idle harvester to quit slacking:
                    playerActor.World.IssueOrder(new Order("Harvest", activeUnit, false));
                }

                Actor[] newUnits = playerActor.World.ActorsWithTrait<IMove>()
                    .Where(a => a.Actor.Owner == aiPlayer && !a.Actor.HasTrait<BaseBuilding>()
                        && !activeUnits.Contains(a.Actor))
                        .Select(a => a.Actor).ToArray();

                foreach (Actor newActor in newUnits)
                {
                    Game.Debug("AI: Found a newly built unit");

                    if (newActor.HasTrait<Harvester>())
                    {
                        playerActor.World.IssueOrder(new Order("Harvest", newActor, false));
                    }
                    else
                    {
                        unitsHangingAroundTheBase.Add(newActor);
                    }

                    activeUnits.Add(newActor);
                }

                bool hasUnit = unitsHangingAroundTheBase.Where(x => x.Info.Name == "e3").Any();

                /* Create an attack force when we have enough units around our base. */
                // (don'position bother leaving any behind for defense.)

                int randomizedSquadSize = this.squadSize - 4 + random.Next(200);

                if (unitsHangingAroundTheBase.Count >= randomizedSquadSize)
                {
                    Game.Debug("Launch an attack.");

                    if (attackForce.Count == 0)
                    {
                        attackTarget = ChooseEnemyTarget(playerActor, aiPlayer);

                        if (attackTarget == null)
                        {
                            return;
                        }

                        foreach (Actor lazyUnit in unitsHangingAroundTheBase)
                        {
                            if (TryToMove(lazyUnit, attackTarget.Value, true))
                            {
                                attackForce.Add(lazyUnit);
                            }
                        }

                        unitsHangingAroundTheBase.Clear();
                    }
                }

                // If we have any attackers, let them scan for enemy units and stop and regroup if they spot any
                if (attackForce.Count > 0)
                {
                    bool foundEnemy = false;

                    foreach (Actor attacker1 in attackForce)
                    {
                        List<Actor> enemyUnits = playerActor.World
                            .FindUnitsInCircle(attacker1.CenterLocation, Game.CellSize * 10)
                            .Where(unit => aiPlayer.Stances[unit.Owner] == Stance.Enemy && !unit.HasTrait<Husk>())
                            .ToList();

                        if (enemyUnits.Count > 0)
                        {
                            // Found enemy units nearby.
                            foundEnemy = true;

                            Actor enemy = enemyUnits.ClosestTo(attacker1.CenterLocation);

                            // Check how many own units we have gathered nearby...
                            List<Actor> ownUnits = playerActor.World
                                .FindUnitsInCircle(attacker1.CenterLocation, Game.CellSize * 2)
                                .Where(unit => unit.Owner == aiPlayer)
                                .ToList();

                            if (ownUnits.Count < randomizedSquadSize)
                            {
                                // Not enough to attack. Send more units.
                                playerActor.World.IssueOrder(new Order("Stop", attacker1, false));

                                foreach (Actor attacker2 in attackForce)
                                {
                                    if (attacker2 != attacker1)
                                    {
                                        playerActor.World.IssueOrder(new Order("AttackMove", attacker2, false) { TargetLocation = attacker1.Location });
                                    }
                                }
                            }
                            else
                            {
                                // We have gathered sufficient units. Attack the nearest enemy unit.
                                foreach (Actor attacker2 in attackForce)
                                {
                                    playerActor.World.IssueOrder(new Order("Attack", attacker2, false) { TargetActor = enemy });
                                }
                            }

                            return;
                        }
                    }

                    if (!foundEnemy)
                    {
                        attackTarget = ChooseEnemyTarget(playerActor, aiPlayer);

                        if (attackTarget != null)
                        {
                            foreach (Actor attacker in attackForce)
                            {
                                TryToMove(attacker, attackTarget.Value, true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Choose among all enemies a valid target
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <returns></returns>
        private CPos? ChooseEnemyTarget(Actor playerActor, Player aiPlayer)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (aiPlayer == null)
            {
                throw new ArgumentNullException("aiPlayer");
            }
            if (this.statistics == null)
            {
                throw new InvalidOperationException("HackyAIAttackStrategy.statistics is not set");
            }

            CPos? result = null;

            IEnumerable<Player> liveEnemies = playerActor.World.Players
                .Where(q => aiPlayer != q && aiPlayer.Stances[q] == Stance.Enemy)
                .Where(q => aiPlayer.WinState == WinState.Undefined && q.WinState == WinState.Undefined);

            IGrouping<int, Player> leastLikedEnemies = liveEnemies
                .GroupBy(e => statistics.Aggressors[e].ProducedDamage)
                .OrderByDescending(g => g.Key)
                .FirstOrDefault();

            if (leastLikedEnemies != null)
            {
                Player enemy = leastLikedEnemies.Random(random);

                /* pick something worth attacking owned by that player */
                IEnumerable<Actor> targets = playerActor.World.Actors
                    .Where(a => a.Owner == enemy && a.HasTrait<IOccupySpace>() && !a.HasTrait<Husk>());

                Actor target = null;

                if (targets.Any())
                {
                    target = targets.Random(random);
                }

                if (target == null)
                {
                    /* Assume that "enemy" has nothing. Cool off on attacks. */
                    statistics.Aggressors[enemy].ProducedDamage = statistics.Aggressors[enemy].ProducedDamage / 2 - 1;

                    Log.Write("debug", "Bot {0} couldn't find target for player {1}", aiPlayer.ClientIndex, enemy.ClientIndex);

                    result = null;
                }
                else
                {
                    /* bump the aggro slightly to avoid changing our mind */
                    if (leastLikedEnemies.Count() > 1)
                    {
                        statistics.Aggressors[enemy].ProducedDamage++;
                    }

                    result = target.Location;
                }
            }

            return result;
        }

        //try very hard to find a valid move destination near the target.
        //(Don'position accept a move onto the subject's current position. maybe this is already not allowed? )
        /// <summary>
        /// Issue the move towards a destination.
        /// </summary>
        /// <param name="actorToMove"></param>
        /// <param name="desiredMoveTarget"></param>
        /// <param name="attackMove"></param>
        /// <returns></returns>
        private bool TryToMove(Actor actorToMove, CPos desiredMoveTarget, bool attackMove)
        {
            if (actorToMove == null)
            {
                throw new ArgumentNullException("actorToMove");
            }

            bool result = false;

            CPos? xy = ChooseDestinationNear(actorToMove, desiredMoveTarget);

            if (xy != null)
            {
                actorToMove.World.IssueOrder(new Order(attackMove ? "AttackMove" : "Move", actorToMove, false) { TargetLocation = xy.Value });

                result = true;
            }
            
            return result;
        }

        /// <summary>
        /// identify a location close by an actor
        /// </summary>
        /// <param name="actorToMove"></param>
        /// <param name="desiredMoveTarget"></param>
        /// <returns></returns>
        private CPos? ChooseDestinationNear(Actor actorToMove, CPos desiredMoveTarget)
        {
            if (actorToMove == null)
            {
                throw new ArgumentNullException("actorToMove");
            }
            if (desiredMoveTarget == null)
            {
                throw new ArgumentNullException("desiredMoveTarget");
            }

            CPos? result = null;

            IMove moveTrait = actorToMove.TraitOrDefault<IMove>();

            if (moveTrait != null)
            {
                CPos? xy;

                int loopCount = 0; //avoid infinite loops.

                int range = 2;

                do
                {
                    //loop until we find a valid move location
                    xy = new CPos(desiredMoveTarget.X + random.Next(-range, range), desiredMoveTarget.Y + random.Next(-range, range));

                    loopCount++;

                    range = Math.Max(range, loopCount / 2);

                    if (loopCount > 10)
                    {
                        xy = null;

                        break;
                    }
                }  while (!moveTrait.CanEnterCell(xy.Value) && xy != actorToMove.Location);

                result = xy;
            }

            return result;
        }
        #endregion
    }
}
