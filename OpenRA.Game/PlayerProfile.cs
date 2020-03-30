#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA
{
	public class PlayerProfile
	{
		public readonly string Fingerprint;
		public readonly string PublicKey;
		public readonly bool KeyRevoked;

		public readonly int ProfileID;
		public readonly string ProfileName;
		public readonly string ProfileRank = "Registered Player";

		[FieldLoader.LoadUsing("LoadBadges")]
		public readonly List<PlayerBadge> Badges;

		static object LoadBadges(MiniYaml yaml)
		{
			var badges = new List<PlayerBadge>();

			var badgesNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Badges");
			if (badgesNode != null)
			{
				try
				{
					var playerDatabase = Game.ModData.Manifest.Get<PlayerDatabase>();
					foreach (var badgeNode in badgesNode.Value.Nodes)
					{
						var badge = playerDatabase.LoadBadge(badgeNode.Value);
						if (badge != null)
							badges.Add(badge);
					}
				}
				catch
				{
					// Discard badges on error
				}
			}

			return badges;
		}
	}

	public class PlayerBadge
	{
		public readonly string Label;
		public readonly Sprite Icon24;

		public PlayerBadge(string label, Sprite icon24)
		{
			Label = label;
			Icon24 = icon24;
		}
	}
}
