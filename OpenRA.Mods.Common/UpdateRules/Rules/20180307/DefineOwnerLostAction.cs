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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class DefineOwnerLostAction : UpdateRule
	{
		public override string Name { get { return "Add OwnerLostAction to player-controlled actors"; } }
		public override string Description
		{
			get
			{
				return "A new OwnerLostAction trait has been introduced to control what happens to a\n" +
				"player's actors when they are defeated. A warning is displayed notifying that\nthis trait must be added to actors.";
			}
		}

		bool reported;
		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!reported)
				yield return "All player-controlled (or player-capturable) actors should define an OwnerLostAction trait\n" +
					"specifying an action (Kill, Dispose, ChangeOwner) to apply when the actor's owner is defeated.\n" +
					"You must manually define this trait on the appropriate default actor templates.\n" +
					"Actors missing this trait will remain controllable by their owner after they have been defeated.";

			reported = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			yield break;
		}
	}
}
