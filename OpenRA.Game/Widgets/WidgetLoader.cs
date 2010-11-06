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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA
{
	public class WidgetLoader
	{
		//        foreach( var file in Game.modData.Manifest.ChromeLayout.Select( a => MiniYaml.FromFile( a ) ) )
		//            foreach( var w in file )
		//                rootWidget.AddChild( WidgetLoader.LoadWidget( w ) );

		//        rootWidget.Initialize();
		//        rootWidget.InitDelegates();

		Dictionary<string, MiniYamlNode> widgets = new Dictionary<string, MiniYamlNode>();

		public WidgetLoader( ModData modData )
		{
			foreach( var file in modData.Manifest.ChromeLayout.Select( a => MiniYaml.FromFile( a ) ) )
				foreach( var w in file )
					widgets.Add( w.Key.Substring( w.Key.IndexOf( '@' ) + 1 ), w );
		}

		public Widget LoadWidget( Dictionary<string, object> args, Widget parent, string w )
		{
			return LoadWidget( args, parent, widgets[ w ] );
		}

		public Widget LoadWidget( Dictionary<string, object> args, Widget parent, MiniYamlNode node)
		{
			var widget = NewWidget(node.Key, args);
			parent.AddChild( widget );

			foreach (var child in node.Value.Nodes)
				if (child.Key != "Children")
					FieldLoader.LoadField(widget, child.Key, child.Value.Value);

			widget.Initialize();

			foreach (var child in node.Value.Nodes)
				if (child.Key == "Children")
					foreach (var c in child.Value.Nodes)
						LoadWidget( args, widget, c);

			widget.PostInit( args );
			return widget;
		}

		Widget NewWidget(string widgetType, Dictionary<string, object> args)
		{
			widgetType = widgetType.Split('@')[0];
			return Game.modData.ObjectCreator.CreateObject<Widget>(widgetType + "Widget", args);
		}
	}
}