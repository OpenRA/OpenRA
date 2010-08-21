using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	class SyncReport
	{
		Queue<Pair<int, string>> syncReports = new Queue<Pair<int, string>>();
		const int numSyncReports = 5;

		internal void UpdateSyncReport()
		{
			if (!Game.Settings.RecordSyncReports)
				return;

			while (syncReports.Count >= numSyncReports) syncReports.Dequeue();
			syncReports.Enqueue(Pair.New(Game.orderManager.FrameNumber, GenerateSyncReport()));
		}

		string GenerateSyncReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Actors:");
			foreach (var a in Game.world.Actors)
				sb.AppendLine("\t {0} {1} {2} ({3})".F(
					a.ActorID,
					a.Info.Name,
					(a.Owner == null) ? "null" : a.Owner.InternalName,
					Sync.CalculateSyncHash(a)));

			sb.AppendLine("Tick Actors:");
			foreach (var a in Game.world.Queries.WithTraitMultiple<object>())
			{
				var sync = Sync.CalculateSyncHash(a.Trait);
				if (sync != 0)
					sb.AppendLine("\t {0} {1} {2} {3} ({4})".F(
						a.Actor.ActorID,
						a.Actor.Info.Name,
						(a.Actor.Owner == null) ? "null" : a.Actor.Owner.InternalName,
						a.Trait.GetType().Name,
						sync));
			}

			return sb.ToString();
		}

		internal void DumpSyncReport(int frame)
		{
			var f = syncReports.FirstOrDefault(a => a.First == frame);
			if (f == default(Pair<int, string>))
			{
				Log.Write("sync", "No sync report available!");
				return;
			}

			Log.Write("sync", "Sync for net frame {0} -------------", f.First);
			Log.Write("sync", "{0}", f.Second);
		}
	}
}
