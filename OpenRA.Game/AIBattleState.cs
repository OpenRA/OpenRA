#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA
{
	/// <summary>
	/// Static state tracking for AI Battle mode.
	/// AI Battle mode allows watching AI-vs-AI battles at accelerated speeds.
	/// </summary>
	public static class AIBattleState
	{
		/// <summary>True when an AI Battle simulation is active.</summary>
		public static bool IsAIBattle;

		/// <summary>Speed multiplier for the simulation (1, 2, 4, 8, 16).</summary>
		public static int SpeedMultiplier = 1;

		/// <summary>Base timestep of the game (usually 40ms).</summary>
		public static int BaseTimestep = 40;

		/// <summary>True when the AI Battle is paused.</summary>
		public static bool IsPaused;

		/// <summary>
		/// Resets all AI Battle state to defaults.
		/// Call this when exiting AI Battle mode.
		/// </summary>
		public static void Reset()
		{
			IsAIBattle = false;
			SpeedMultiplier = 1;
			BaseTimestep = 40;
			IsPaused = false;
		}

		/// <summary>
		/// Gets the effective timestep for the current speed settings.
		/// Lower timestep = faster game speed. Minimum is 1ms.
		/// Returns 0 when paused (like replays).
		/// For speeds > 32x, use GetTicksPerFrame() to run multiple ticks.
		/// </summary>
		public static int GetEffectiveTimestep()
		{
			if (IsPaused)
				return 0; // 0 means paused (same semantics as ReplayTimestep)

			// For very high speeds, we hit the 1ms floor
			// The extra speedup comes from running multiple ticks per frame
			return Math.Max(1, BaseTimestep / SpeedMultiplier);
		}

		/// <summary>
		/// Gets the number of logic ticks to run per frame.
		/// For speeds up to 32x, this returns 1.
		/// For speeds above 32x, this returns extra ticks to compensate for the 1ms timestep floor.
		/// Example: At 64x with 40ms base, we want 64x speedup but timestep floors at 1ms (32x).
		/// So we run 2 ticks per frame to achieve the full 64x speed.
		/// </summary>
		public static int GetTicksPerFrame()
		{
			if (IsPaused)
				return 0;

			// Calculate how many ticks we need to achieve the desired speed
			// At 32x: 40/32 = 1ms timestep, 1 tick per frame = 32x speed
			// At 64x: 40/64 = 0.625 -> floors to 1ms, need 2 ticks per frame = 64x speed
			// At 128x: need 4 ticks per frame
			// At 256x: need 8 ticks per frame
			var idealTimestep = BaseTimestep / (float)SpeedMultiplier;
			if (idealTimestep >= 1)
				return 1;

			// Return how many extra ticks we need
			return (int)Math.Ceiling(1.0 / idealTimestep);
		}
	}

	/// <summary>
	/// Static state for replay rewind operations.
	/// Rewind works by restarting the replay and fast-forwarding to the target tick.
	/// This approach avoids needing full state serialization for instant rewind.
	/// </summary>
	public static class AIBattleRewindState
	{
		/// <summary>True when a rewind operation is in progress.</summary>
		public static bool IsRewinding;

		/// <summary>The target tick to reach after restart.</summary>
		public static int TargetTick;

		/// <summary>The render player's internal name to restore after reaching target.</summary>
		public static string RestoreRenderPlayer;

		/// <summary>Resets all rewind state.</summary>
		public static void Reset()
		{
			IsRewinding = false;
			TargetTick = 0;
			RestoreRenderPlayer = null;
		}
	}

	/// <summary>
	/// Static state for fast-forward operations during replay.
	/// Used for both seeking forward and the fast-forward phase of rewind.
	/// </summary>
	public static class AIBattleFastForwardState
	{
		/// <summary>True when fast-forwarding to a target tick.</summary>
		public static bool IsFastForwarding;

		/// <summary>The target tick to reach.</summary>
		public static int TargetTick;

		/// <summary>Resets all fast-forward state.</summary>
		public static void Reset()
		{
			IsFastForwarding = false;
			TargetTick = 0;
		}
	}
}
