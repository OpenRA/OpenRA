using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Linq;

namespace OpenRA
{
	class WidgetLoader
	{
		public static Widget rootWidget;
		public static Widget LoadWidget( MiniYaml node )
		{
			var widget = NewWidget( node.Value );
			foreach( var child in node.Nodes )
			{
				if( child.Key == "Children" )
				{
					foreach( var c in child.Value.Nodes )
					{
						// Hack around a bug in MiniYaml
						c.Value.Value = c.Key;
						widget.AddChild( LoadWidget( c.Value ) );
					}
				}
				else
					FieldLoader.LoadField( widget, child.Key, child.Value );
			}
			return widget;
		}
			
		static Widget NewWidget( string widgetType )
		{
			if( widgetType.Contains( "@" ) )
				widgetType = widgetType.Substring( 0, widgetType.IndexOf( "@" ) );
			
			foreach (var mod in Game.ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + widgetType + "Widget";
				var widget = (Widget)mod.First.CreateInstance(fullTypeName);
				if (widget == null) continue;
				
				Log.Write("Creating Widget of type {0}",widgetType);
				return widget;
			}

			throw new InvalidOperationException("Cannot locate widget: {0}".F(widgetType));
		}
	}
}