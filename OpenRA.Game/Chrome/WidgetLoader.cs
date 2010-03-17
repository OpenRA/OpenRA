using System;
using OpenRA.FileFormats;
using OpenRA.Widgets;
using System.Collections.Generic;

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