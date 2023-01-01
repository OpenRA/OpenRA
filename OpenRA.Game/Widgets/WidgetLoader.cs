#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA
{
	public class WidgetLoader
	{
		readonly Dictionary<string, MiniYamlNode> widgets = new Dictionary<string, MiniYamlNode>();
		readonly ModData modData;

		public WidgetLoader(ModData modData)
		{
			this.modData = modData;

			foreach (var file in modData.Manifest.ChromeLayout.Select(a => MiniYaml.FromStream(modData.DefaultFileSystem.Open(a), a)))
				foreach (var w in file)
				{
					var key = w.Key.Substring(w.Key.IndexOf('@') + 1);
					if (widgets.ContainsKey(key))
						throw new InvalidDataException($"Widget has duplicate Key `{w.Key}` at {w.Location}");
					widgets.Add(key, w);
				}
		}

		public Widget LoadWidget(WidgetArgs args, Widget parent, string w)
		{
			if (!widgets.TryGetValue(w, out var ret))
				throw new InvalidDataException($"Cannot find widget with Id `{w}`");

			return LoadWidget(args, parent, ret);
		}

		public Widget LoadWidget(WidgetArgs args, Widget parent, MiniYamlNode node)
		{
			if (!args.ContainsKey("modData"))
				args = new WidgetArgs(args) { { "modData", modData } };

			var widget = NewWidget(node.Key, args);

			parent?.AddChild(widget);

			if (node.Key.Contains('@'))
				FieldLoader.LoadField(widget, "Id", node.Key.Split('@')[1]);

			foreach (var child in node.Value.Nodes)
				if (child.Key != "Children")
					FieldLoader.LoadField(widget, child.Key, child.Value.Value);

			widget.Initialize(args);

			foreach (var child in node.Value.Nodes)
				if (child.Key == "Children")
					foreach (var c in child.Value.Nodes)
						LoadWidget(args, widget, c);

			var logicNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Logic");
			var logic = logicNode?.Value.ToDictionary();
			args.Add("logicArgs", logic);

			widget.PostInit(args);

			args.Remove("logicArgs");

			return widget;
		}

		static Widget NewWidget(string widgetType, WidgetArgs args)
		{
			widgetType = widgetType.Split('@')[0];
			return Game.ModData.ObjectCreator.CreateObject<Widget>(widgetType + "Widget", args);
		}
	}
}
