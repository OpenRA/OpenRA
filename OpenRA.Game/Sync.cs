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
using OpenRA.Traits;

namespace OpenRA
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class GenerateSyncCodeAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class SyncMemberAttribute : Attribute { }

	public interface ISync
	{
		int GetSyncHash();
	}

	public static class Sync
	{
		public static int Hash(bool a) => a ? 0xaaa : 0x555;

		public static int Hash(int a) => a;

		public static int Hash(int2 a) => ((a.X * 5) ^ (a.Y * 3)) / 4;

		public static int Hash(CPos a) => a.Bits;

		public static int Hash(CVec a) => ((a.X * 5) ^ (a.Y * 3)) / 4;

		public static int Hash(WDist a) => a.Length;

		public static int Hash(WPos a) => a.X ^ a.Y ^ a.Z;

		public static int Hash(WVec a) => a.X ^ a.Y ^ a.Z;

		public static int Hash(WAngle a) => a.Angle;

		public static int Hash(WRot a) => Hash(a.Roll) ^ Hash(a.Pitch.Angle) ^ Hash(a.Yaw.Angle);

		public static int Hash(Actor a) => a == null ? 0 : (int)(a.ActorID << 16);

		public static int Hash(Player p) => p == null ? 0 : (int)(p.PlayerActor.ActorID << 16) * 0x567;

		public static int Hash(object o) => throw new NotSupportedException($"Type {o.GetType().FullName} is not supported for Sync hashing!");

		public static int Hash(Target t)
		{
			switch (t.Type)
			{
				case TargetType.Actor:
					return Hash(t.Actor);

				case TargetType.FrozenActor:
					return Hash(t.FrozenActor.Actor);

				case TargetType.Terrain:
					return Hash(t.CenterPosition);

				case TargetType.Invalid:
				default:
					return 0;
			}
		}

		public static void RunUnsynced(World world, Action fn)
		{
			RunUnsynced(Game.Settings.Debug.SyncCheckUnsyncedCode, world, () => { fn(); return true; });
		}

		public static void RunUnsynced(bool checkSyncHash, World world, Action fn)
		{
			RunUnsynced(checkSyncHash, world, () => { fn(); return true; });
		}

		static int unsyncCount = 0;

		public static T RunUnsynced<T>(World world, Func<T> fn)
		{
			return RunUnsynced(Game.Settings.Debug.SyncCheckUnsyncedCode, world, fn);
		}

		public static T RunUnsynced<T>(bool checkSyncHash, World world, Func<T> fn)
		{
			unsyncCount++;

			// Detect sync changes in top level entry point only. Do not recalculate sync hash during reentry.
			var sync = unsyncCount == 1 && checkSyncHash && world != null ? world.SyncHash() : 0;

			// Running this inside a try with a finally statement means unsyncCount is decremented as soon as fn completes
			try
			{
				return fn();
			}
			finally
			{
				unsyncCount--;

				// When the world is disposing all actors and effects have been removed
				// So do not check the hash for a disposing world since it definitively has changed
				if (unsyncCount == 0 && checkSyncHash && world != null && !world.Disposing && sync != world.SyncHash())
					throw new InvalidOperationException("RunUnsynced: sync-changing code may not run here");
			}
		}

		public static void AssertUnsynced(string message)
		{
			if (unsyncCount == 0)
				throw new InvalidOperationException(message);
		}
	}
}
