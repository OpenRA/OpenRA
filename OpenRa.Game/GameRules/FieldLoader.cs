using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game.GameRules
{
	static class FieldLoader
	{
		public  static void Load( object self, IniSection ini )
		{
			foreach( var x in ini )
			{
				var field = self.GetType().GetField( x.Key );
				field.SetValue( self, GetValue( field.FieldType, x.Value ) );
			}
		}

		static object GetValue( Type fieldType, string x )
		{
			if( fieldType == typeof( int ) )
				return int.Parse( x );

			else if (fieldType == typeof(float))
				return float.Parse(x.Replace("%","")) * (x.Contains( '%' ) ? 0.01f : 1f);

			else if (fieldType == typeof(string))
				return x;//.ToLowerInvariant();

			else if (fieldType.IsEnum)
				return Enum.Parse(fieldType, x);

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
