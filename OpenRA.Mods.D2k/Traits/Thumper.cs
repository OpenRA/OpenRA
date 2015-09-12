#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Lures the `Sandworm` to its position when deployed. To be used in conjunction with an `AttractsWorm` upgrade.")]
	class ThumperInfo : ITraitInfo, Requires<MobileInfo>, Requires<UpgradeManagerInfo>
	{
		[SequenceReference(null, true)] public readonly string UndeployedPrefix = "undeployed-";

		public readonly int ThumpInterval = 40;
		public readonly string ThumpSound = "THUMPER1.WAV";

		public readonly string[] DeployTerrainTypes = { "Sand", "Dune", "Spice" };
		public readonly string DeployCursor = "deploy";
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		[UpgradeGrantedReference]
		[Desc("The upgrade to grant when deploying and revoke when undeploying.")]
		public readonly string Upgrade = "WormAttraction";

		public object Create(ActorInitializer init) { return new Thumper(init.Self, this); }
	}

	class Thumper : ITick, IIssueOrder, IResolveOrder, IOrderVoice, IRenderInfantrySequenceModifier
	{
		readonly Actor self;
		readonly ThumperInfo info;
		readonly Mobile mobile;
		readonly UpgradeManager manager;

		bool deployed;
		bool isUpgraded;
		int tick;

		public bool IsModifyingSequence { get { return !deployed; } }
		public string SequencePrefix { get { return info.UndeployedPrefix; } }

		public Thumper(Actor self, ThumperInfo info)
		{
			this.self = self;
			this.info = info;
			mobile = self.Trait<Mobile>();
			manager = self.Trait<UpgradeManager>();
		}

		public void Tick(Actor self)
		{
			if (!deployed)
				return;

			if (++tick >= info.ThumpInterval)
			{
				tick = 0;
				Sound.Play(info.ThumpSound, self.CenterPosition);
			}
		}

		bool CanDeploy()
		{
			var terrainType = self.World.Map.GetTerrainInfo(self.Location).Type;
			return info.DeployTerrainTypes.Contains(terrainType) && !mobile.IsMoving;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter("DeployThumper", 5,
					() => CanDeploy() ? info.DeployCursor : info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployThumper")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployThumper") ? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!CanDeploy())
				return;

			deployed = order.OrderString == "DeployThumper";
			if (deployed && !isUpgraded)
			{
				isUpgraded = true;
				manager.GrantUpgrade(self, info.Upgrade, this);
			}
			else if (!deployed && isUpgraded)
			{
				manager.RevokeUpgrade(self, info.Upgrade, this);
				isUpgraded = false;
			}

			tick = 0;
		}
	}
}
