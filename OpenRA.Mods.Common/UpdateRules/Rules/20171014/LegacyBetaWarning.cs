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
	public class LegacyBetaWarning : UpdateRule
	{
		public override string Name { get { return "Update path from 20171014 to 20180307 is in beta state"; } }
		public override string Description
		{
			get
			{
				return "Due to time constraints and the legacy status of the included rules,\n" +
					"the update path to 20180307 is considered a beta path.\n" +
					"If you encounter any issues, please report them on GitHub or our IRC channel.";
			}
		}

		bool displayed;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "Due to time constraints and the legacy status of the included rules,\n" +
					"the update path to 20180307 is considered a beta path.\n" +
					"If you encounter any issues, please report them on GitHub or our IRC channel.";

			if (!displayed)
				yield return message;

			displayed = true;
		}
	}
}
