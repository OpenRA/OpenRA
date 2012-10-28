using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// A strategy applied to handle or react on damage
    /// </summary>
    internal interface IDamageStrategy
    {
        /// <summary>
        /// Notifies the strategy about the damage.
        /// </summary>
        /// <param name="damagedActor"></param>
        /// <param name="e"></param>
        void Damaged(Actor damagedActor, AttackInfo e);
    }
}
