using System;
using System.Linq;
using OpenRa.FileFormats;
using System.Collections.Generic;

namespace OpenRa.Game.GameRules
{
	static class FieldLoader
	{
		public  static void Load( object self, IniSection ini )
		{
			foreach( var x in ini )
			{
				var field = self.GetType().GetField( x.Key.Trim() );
				field.SetValue( self, GetValue( field.FieldType, x.Value.Trim() ) );
			}
		}

		public static void Load(object self, MiniYaml my)
		{
			foreach (var x in my.Nodes)
			{
				var field = self.GetType().GetField(x.Key.Trim());
				field.SetValue(self, GetValue(field.FieldType, x.Value.Value.Trim()));
			}
		}

		public static void CheckYaml( object self, Dictionary<string, MiniYaml> d )
		{
			//foreach( var x in d )
			//{
			//    if( x.Key == "Tab" ) continue;
			//    if( x.Key == "Description" ) continue;
			//    if( x.Key == "LongDesc" ) continue;

			//    var key = x.Key;
			//    if( key == "Prerequisites" ) key = "Prerequisite";
			//    if( key == "HP" ) key = "Strength";
			//    if( key == "Priority" ) key = "SelectionPriority";
			//    if( key == "Bounds" ) key = "SelectionSize";
			//    var field = self.GetType().GetField( key );
			//    var old = field.GetValue( self );
			//    var neww = GetValue( field.FieldType, x.Value.Value.Trim() );
			//    if( old.ToString() != neww.ToString() )
			//        throw new NotImplementedException();
			//}
			foreach( var x in d )
			{
				var key = x.Key;
				if( key == "Tab" )
					continue;
				if( key == "Prerequisites" ) key = "Prerequisite";
				if( key == "HP" ) key = "Strength";
				if( key == "Priority" ) key = "SelectionPriority";
				if( key == "Bounds" ) key = "SelectionSize";
				var field = self.GetType().GetField( key.Trim() );
				field.SetValue( self, GetValue( field.FieldType, x.Value.Value.Trim() ) );
			}
		}

		static object GetValue( Type fieldType, string x )
		{
			if( fieldType == typeof( int ) )
				return int.Parse( x );

			else if (fieldType == typeof(float))
				return float.Parse(x.Replace("%","")) * (x.Contains( '%' ) ? 0.01f : 1f);

			else if (fieldType == typeof(string))
				return x;

			else if (fieldType.IsEnum)
				return Enum.Parse(fieldType, x, true);

			else if (fieldType == typeof(bool))
				return ParseYesNo(x);

			else if (fieldType.IsArray)
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

				var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
				for (int i = 0; i < parts.Length; i++)
					ret.SetValue(GetValue(fieldType.GetElementType(), parts[i].Trim()), i);
				return ret;
			}
			else if (fieldType == typeof(int2))
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new int2(int.Parse(parts[0]), int.Parse(parts[1]));
			}
			else
				throw new InvalidOperationException("FieldLoader: don't know how to load field of type " + fieldType.ToString());
		}

		static bool ParseYesNo( string p )
		{
			p = p.ToLowerInvariant();
			if( p == "yes" ) return true;
			if( p == "true" ) return true;
			if( p == "no" ) return false;
			if( p == "false" ) return false;
			throw new InvalidOperationException();
		}
	}
}
