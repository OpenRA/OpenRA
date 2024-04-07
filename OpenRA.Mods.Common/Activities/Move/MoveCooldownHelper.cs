using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	/// <summary>
	/// Activities that queue move activities via <see cref="IMove"/> can use this helper to decide
	/// when moves with blocked destinations should be retried and to apply a cooldown between repeated moves.
	/// </summary>
	public sealed class MoveCooldownHelper
	{
		/// <summary>
		/// If a move failed because the destination was blocked, indicates if we should try again.
		/// When true, <see cref="Tick"/> will return null when the destination is blocked, after the cooldown has been applied.
		/// When false, <see cref="Tick"/> will return true to indicate the activity should give up and complete.
		/// Defaults to false.
		/// </summary>
		public bool RetryIfDestinationBlocked { get; set; }

		/// <summary>
		/// The cooldown delay in ticks. After a move with a blocked destination, the cooldown will be started.
		/// Whilst the cooldown is in effect, <see cref="Tick"/> will return false.
		/// After the cooldown finishes, <see cref="Tick"/> will return null to allow activity logic to resume.
		/// This cooldown is important to avoid lag spikes caused by pathfinding every tick because the destination is unreachable.
		/// Defaults to (20, 31).
		/// </summary>
		public (int MinTicksInclusive, int MaxTicksExclusive) Cooldown { get; set; } = (20, 31);

		readonly World world;
		readonly Mobile mobile;
		bool wasMoving;
		bool hasRunCooldown;
		int cooldownTicks;

		public MoveCooldownHelper(World world, Mobile mobile)
		{
			this.world = world;
			this.mobile = mobile;
		}

		/// <summary>
		/// Call this when queuing a move activity.
		/// </summary>
		public void NotifyMoveQueued()
		{
			wasMoving = true;
		}

		/// <summary>
		/// Call this method within the <see cref="Activity.Tick(Actor)"/> method. It will return a tick result.
		/// </summary>
		/// <param name="targetIsHiddenActor">If the target is a hidden actor, forces the result to be true, once the move has completed.</param>
		/// <returns>A result that should be returned from the calling Tick method.
		/// A non-null result should be returned immediately.
		/// On a null result, the method should continue with it's usual logic and perform any desired moves.</returns>
		public bool? Tick(bool targetIsHiddenActor)
		{
			// We haven't moved yet, or we did move and we've finished the cooldown, allow the caller to resume with their logic.
			if (!wasMoving)
				return null;

			if (!hasRunCooldown)
			{
				// The target is hidden, don't continue tracking it.
				if (targetIsHiddenActor)
					return true;

				// Movement was cancelled, or we reached our destination, return immediately to allow the caller to perform their next steps.
				if (mobile == null || mobile.MoveResult == MoveResult.CompleteCanceled || mobile.MoveResult == MoveResult.CompleteDestinationReached)
				{
					wasMoving = false;
					return null;
				}

				// We couldn't reach the destination, don't try and keep going after the actor.
				if (!RetryIfDestinationBlocked && mobile.MoveResult == MoveResult.CompleteDestinationBlocked)
					return true;

				// To avoid excessive pathfinding when the destination is blocked, wait for the cooldown before trying to move again.
				// Applying some jitter to the wait time helps avoid multiple units repathing on the same tick and creating a lag spike.
				hasRunCooldown = true;
				cooldownTicks = world.SharedRandom.Next(Cooldown.MinTicksInclusive, Cooldown.MaxTicksExclusive);
				return false;
			}
			else
			{
				if (cooldownTicks > 0)
					cooldownTicks--;

				if (cooldownTicks <= 0)
				{
					hasRunCooldown = false;
					wasMoving = false;
				}

				return false;
			}
		}
	}
}
