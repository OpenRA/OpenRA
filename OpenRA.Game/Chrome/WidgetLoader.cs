using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;
using OpenRA.Widgets.Actions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace OpenRA
{
	class WidgetLoader
	{
		static Pair<Assembly, string>[] ModAssemblies;
		public static Pair<Assembly, string>[] WidgetActionAssemblies;
		public static void LoadModAssemblies(Manifest m)
		{
			var asms = new List<Pair<Assembly, string>>();

			// all the core stuff is in this assembly
			asms.Add(Pair.New(typeof(Widget).Assembly, typeof(Widget).Namespace));

			// add the mods
			foreach (var a in m.Assemblies)
				asms.Add(Pair.New(Assembly.LoadFile(Path.GetFullPath(a)), Path.GetFileNameWithoutExtension(a)));
			ModAssemblies = asms.ToArray();
			
			asms.Clear();

			// all the core stuff is in this assembly
			asms.Add(Pair.New(typeof(IWidgetAction).Assembly, typeof(IWidgetAction).Namespace));

			// add the mods
			foreach (var a in m.Assemblies)
				asms.Add(Pair.New(Assembly.LoadFile(Path.GetFullPath(a)), Path.GetFileNameWithoutExtension(a)));
			WidgetActionAssemblies = asms.ToArray();
		}
		
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
			widget.Initialize();
			return widget;
		}
	
		static Widget NewWidget( string widgetType )
		{
			if( widgetType.Contains( "@" ) )
				widgetType = widgetType.Substring( 0, widgetType.IndexOf( "@" ) );
			
			foreach (var mod in ModAssemblies)
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