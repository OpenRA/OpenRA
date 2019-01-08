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
	public class RemovedAutoCarryallCircleTurnSpeed : UpdateRule
	{
		public override string Name { get { return "Removed AutoCarryall idle circling turnspeed hardcoding"; } }
		public override string Description
		{
			get
			{
				return "Aircraft that circle while idle despite having CanHover (AutoCarryall) have their\n" +
					"turn speed during idle circling no longer hardcoded to 1/3 of regular TurnSpeed." +
					"Note that the new IdleTurnSpeed override works on all aircraft that circle when idle.";
			}
		}

		bool showMessage;
		bool messageShown;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "While circling idle, your AutoCarryall(s) will now turn at full TurnSpeed,\n" +
				"unless you manually define a custom IdleTurnSpeed.";

			if (showMessage && !messageShown)
				yield return message;

			showMessage = false;
			messageShown = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var autoCarryall = actorNode.LastChildMatching("AutoCarryall");
			if (autoCarryall == null)
				yield break;

			showMessage = true;

			yield break;
		}
	}
}
