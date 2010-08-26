#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA
{
	class WidgetLoader
	{
		public static Widget LoadWidget(MiniYamlNode node)
		{
			var widget = NewWidget(node.Key);
			foreach (var child in node.Value.Nodes)
			{
				if (child.Key == "Children")
					foreach (var c in child.Value.Nodes)
						widget.AddChild(LoadWidget(c));
				else
					FieldLoader.LoadField(widget, child.Key, child.Value.Value);
			}
			return widget;
		}

		static Widget NewWidget(string widgetType)
		{
			widgetType = widgetType.Split('@')[0];
			return Game.CreateObject<Widget>(widgetType + "Widget");
		}
	}
}