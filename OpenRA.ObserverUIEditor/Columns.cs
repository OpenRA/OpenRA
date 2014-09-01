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
using OpenRA.FileFormats;

namespace OpenRA.ObserverUIEditor
{
	public class BaseColumn
	{
		public MiniYamlNode RowNode;

		public BaseColumn()
		{
		}
	}

	public class TableColumn : BaseColumn
	{
		public MiniYamlNode HeaderNode;

		public override string ToString()
		{
			var colheadertitle = NodeUtils.GetTextValue(HeaderNode, "Text");
			var colheaderwidth = NodeUtils.GetIntValue(HeaderNode, "Width");

			return colheadertitle + " (" + colheaderwidth + ") => " + RowNode.Key;
		}
	}

	public class BarstatsColumn : BaseColumn
	{
		public override string ToString()
		{
			var colheadertitle = NodeUtils.GetTextValue(RowNode, "Text");
			var colheaderwidth = NodeUtils.GetIntValue(RowNode, "Width");
			var colx = NodeUtils.GetIntValue(RowNode, "X");

			return colheadertitle + "" + colx + ": " + RowNode.Key + " /w " + colheaderwidth;
		}
	}
}
