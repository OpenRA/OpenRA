#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA
{
	class WidgetLoader
	{
		public static Widget LoadWidget(KeyValuePair<string, MiniYaml> node)
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

			foreach (var mod in Game.ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + widgetType + "Widget";
				var widget = (Widget)mod.First.CreateInstance(fullTypeName);
				if (widget == null) continue;

				Log.Write("Creating Widget of type {0}", widgetType);
				return widget;
			}

			throw new InvalidOperationException("Cannot locate widget: {0}".F(widgetType));
		}
	}
}