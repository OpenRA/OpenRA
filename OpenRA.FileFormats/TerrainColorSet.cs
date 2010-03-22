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

using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System;
namespace OpenRA.FileFormats
{
	public class TerrainColorSet
	{
		public readonly Dictionary<TerrainType, Color> colors = new Dictionary<TerrainType, Color>();

		string NextLine( StreamReader reader )
		{
			string ret;
			do
			{
				ret = reader.ReadLine();
				if( ret == null )
					return null;
				ret = ret.Trim();
			}
			while( ret.Length == 0 || ret[ 0 ] == ';' );
			return ret;
		}

		public TerrainColorSet( string colorFile )
		{
			StreamReader file = new StreamReader( FileSystem.Open(colorFile) );

			while( true )
			{
				string line = NextLine( file );
				if( line == null )
					break;
				string[] kv = line.Split('=');
				TerrainType key = (TerrainType)Enum.Parse(typeof(TerrainType),kv[0]);
				string[] entries = kv[1].Split(',');
				Color val = Color.FromArgb(int.Parse(entries[0]),int.Parse(entries[1]),int.Parse(entries[2]));
				colors.Add(key,val);
			}

			file.Close();
		}
		
		public Color ColorForTerrainType(TerrainType type)
		{
			return colors[type];	
		}
	}
}
