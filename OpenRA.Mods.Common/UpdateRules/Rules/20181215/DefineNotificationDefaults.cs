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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class DefineNotificationDefaults : UpdateRule
	{
		public override string Name { get { return "Move mod-specific notifications to yaml"; } }
		public override string Description
		{
			get
			{
				return "Mod-specific default notifications values have been removed from several traits and the Radar widget\n" +
					"(" + traits.Select(f => f.Trait).JoinWith(", ") + ")\n" +
					"The mod chrome is updated automatically and uses of these traits are listed for inspection so the values can be overriden in yaml.";
			}
		}

		class TraitWrapper
		{
			public readonly string Trait;
			public readonly Dictionary<string, string> Fields;
			public List<string> Uses = new List<string>();

			public TraitWrapper(string trait, Dictionary<string, string> fields)
			{
				Trait = trait;
				Fields = fields;
			}
		}

		TraitWrapper[] traits =
		{
			new TraitWrapper("PrimaryBuilding", new Dictionary<string, string>
			{
				{ "SelectionNotification", "PrimaryBuildingSelected" }
			}),
			new TraitWrapper("RepairableBuilding", new Dictionary<string, string>
			{
				{ "RepairingNotification", "Repairing" }
			}),
			new TraitWrapper("RepairsUnits", new Dictionary<string, string>
			{
				{ "StartRepairingNotification", "Repairing" }
			}),
			new TraitWrapper("GainsExperience", new Dictionary<string, string>
			{
				{ "LevelUpNotification", "LevelUp" }
			}),
			new TraitWrapper("MissionObjectives", new Dictionary<string, string>
			{
				{ "WinNotification", "Win" },
				{ "LoseNotification", "Lose" },
				{ "LeaveNotification", "Leave" }
			}),
			new TraitWrapper("PlaceBuilding", new Dictionary<string, string>
			{
				{ "NewOptionsNotification", "NewOptions" },
				{ "CannotPlaceNotification", "BuildingCannotPlaceAudio" }
			}),
			new TraitWrapper("PlayerResources", new Dictionary<string, string>
			{
				{ "CashTickUpNotification", "CashTickUp" },
				{ "CashTickDownNotification", "CashTickDown" }
			}),
			new TraitWrapper("ProductionQueue", new Dictionary<string, string>
			{
				{ "ReadyAudio", "UnitReady" },
				{ "BlockedAudio", "NoBuild" },
				{ "QueuedAudio", "Training" },
				{ "OnHoldAudio", "OnHold" },
				{ "CancelledAudio", "Cancelled" }
			}),
			new TraitWrapper("PowerManager", new Dictionary<string, string>
			{
				{ "SpeechNotification", "LowPower" }
			}),
			new TraitWrapper("Infiltrates", new Dictionary<string, string>
			{
				{ "Notification", "BuildingInfiltrated" }
			})
		};

		string BuildMessage(TraitWrapper t)
		{
			return "Default notification values have been removed from {0}.\n".F(t.Trait) +
				"You may wish to explicitly define the following overrides:\n   " + t.Trait + ":\n" +
				UpdateUtils.FormatMessageList(t.Fields.Select(kv => "   " + kv.Key + ": " + kv.Value), separator: "  ") +
				"\non the following actors (if they have not already been inherited from a parent).\n" +
				UpdateUtils.FormatMessageList(t.Uses);
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			foreach (var t in traits)
			{
				if (t.Uses.Any())
					yield return BuildMessage(t);

				t.Uses.Clear();
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var t in traits)
			{
				foreach (var traitNode in actorNode.ChildrenMatching(t.Trait))
				{
					foreach (var f in t.Fields)
					{
						var node = traitNode.LastChildMatching(f.Key);
						if (node == null)
						{
							var location = "{0} ({1})".F(actorNode.Key, traitNode.Location.Filename);
							if (!t.Uses.Contains(location))
								t.Uses.Add(location);
						}
					}
				}
			}

			yield break;
		}

		public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNode chromeNode)
		{
			foreach (var node in chromeNode.ChildrenMatching("Radar"))
			{
				if (!node.ChildrenMatching("SoundUp").Any())
					node.AddNode("SoundUp", "RadarUp");

				if (!node.ChildrenMatching("SoundDown").Any())
					node.AddNode("SoundDown", "RadarDown");
			}

			yield break;
		}
	}
}
