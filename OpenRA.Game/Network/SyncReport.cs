#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Network
{
	class SyncReport
	{
		readonly OrderManager orderManager;
		const int numSyncReports = 5;
		Report[] syncReports = new Report[numSyncReports];
		int curIndex = 0;

		public SyncReport( OrderManager orderManager )
		{
			this.orderManager = orderManager;
			for (var i = 0; i < numSyncReports; i++)
				syncReports[i] = new SyncReport.Report();
		}

		internal void UpdateSyncReport()
		{
			GenerateSyncReport(syncReports[curIndex]);
			curIndex = ++curIndex % numSyncReports;
		}

		void GenerateSyncReport(Report report)
		{
			report.Frame = orderManager.NetFrameNumber;
			report.SyncedRandom = orderManager.world.SharedRandom.Last;
			report.TotalCount = orderManager.world.SharedRandom.TotalCount;
			report.Traits.Clear();
			foreach (var a in orderManager.world.ActorsWithTrait<ISync>())
			{
				var sync = Sync.CalculateSyncHash(a.Trait);
				if (sync != 0)
					report.Traits.Add(new TraitReport()
					{
						ActorID = a.Actor.ActorID,
						Type = a.Actor.Info.Name,
						Owner = (a.Actor.Owner == null) ? "null" : a.Actor.Owner.PlayerName,
						Trait = a.Trait.GetType().Name,
						Hash = sync
					});
			}
		}

		internal void DumpSyncReport(int frame)
		{
			foreach (var r in syncReports)
				if (r.Frame == frame)
				{
					Log.Write("sync", "Sync for net frame {0} -------------", r.Frame);
					Log.Write("sync", "SharedRandom: {0} (#{1})", r.SyncedRandom, r.TotalCount);
					Log.Write("sync", "Synced Traits:");
					foreach (var a in r.Traits)
						Log.Write("sync", "\t {0} {1} {2} {3} ({4})".F(
							a.ActorID,
							a.Type,
							a.Owner,
							a.Trait,
							a.Hash
						));
					return;
				}
			Log.Write("sync", "No sync report available!");
		}

		class Report
		{
			public int Frame;
			public int SyncedRandom;
			public int TotalCount;
			public List<TraitReport> Traits = new List<TraitReport>();
		}

		struct TraitReport
		{
			public uint ActorID;
			public string Type;
			public string Owner;
			public string Trait;
			public int Hash;
		}

	}
}
