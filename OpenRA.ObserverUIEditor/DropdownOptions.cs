#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.ObserverUIEditor
{
	public class DropDownOption
	{
		public string Caption;
		public string YamlTitle;
		public bool IsTable;
		public bool IsStatsbar;

		public DropDownOption(string name, string yamltitle)
		{
			Caption = name;
			YamlTitle = yamltitle;

			IsTable = YamlTitle.StartsWith("Table@");
			IsStatsbar = YamlTitle.StartsWith("Spacing@OBSERVER_STATS_BOTTOM_PANEL");
		}

		public override string ToString()
		{
			return Caption;
		}
	}
}
