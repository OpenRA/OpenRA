using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game
{
	class ProductionItem
	{
		public readonly string Item;

		public readonly int TotalTime;
		public readonly int TotalCost;
		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }

		public bool Paused = false, Done = false;
		public Action OnComplete;

		public ProductionItem(string item, int time, int cost, Action onComplete)
		{
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
		}

		public void Tick(Player player)
		{
			if (Done)
			{
				if (OnComplete != null) OnComplete();
				return;
			}

			if (Paused) return;

			var costThisFrame = RemainingCost / RemainingTime;
			if (costThisFrame != 0 && !player.TakeCash(costThisFrame)) return;

			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0) return;

			Done = true;
		}
	}
}
