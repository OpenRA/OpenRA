//     ====================================================================================================
//     YAML Parser for the .NET Framework
//     ====================================================================================================
//
//     Copyright (c) 2006
//         Christophe Lambrechts
//         Jonathan Slenders
//
//     ====================================================================================================
//     This file is part of the .NET YAML Parser.
// 
//     This .NET YAML parser is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as published by
//     the Free Software Foundation; either version 2.1 of the License, or
//     (at your option) any later version.
// 
//     The .NET YAML parser is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Lesser General Public License for more details.
// 
//     You should have received a copy of the GNU Lesser General Public License
//     along with Foobar; if not, write to the Free Software
//     Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USAusing System.Reflection;
//     ====================================================================================================

using System;

namespace Yaml
{
	/// <summary>
	///   Class for storing a Yaml Boolean node
	///   tag:yaml.org,2002:bool
	/// </summary>
	public class Boolean : Scalar
	{
		private bool content;
		
		/// <summary> New boolean </summary>
		public Boolean (bool val) :
				base ("tag:yaml.org,2002:bool", NodeType.Boolean)
		{
			content = val;
		}

		/// <summary> Parse boolean </summary>
		public Boolean (ParseStream stream) :
				base ("tag:yaml.org,2002:bool", NodeType.Boolean)
		{
			// Read the first 8 chars
			char [] chars = new char [8];

			int length = 0;
			for (int i = 0; i < chars.Length && ! stream.EOF; i ++)
			{
				chars [i] = stream.Char;
				length ++;
				stream.Next (true);
			}

			// Compare
			if (length == 1)
			{
				if (chars[0] == 'Y' || chars[0] == 'y')
					{ content = true; return; } 

				if (chars[0] == 'N' || chars[0] == 'n')
					{ content = false; return; }
			}
			if (length == 2)
			{
				string s = "" + chars [0] + chars [1];

				if (s == "ON" || s == "On" || s == "on")
					{ content = true; return; }

				if (s == "NO" || s == "No" || s == "no")
					{ content = false; return; }
			}
			if (length == 3)
			{
				string s = "" + chars [0] + chars [1] + chars [2];

				if (s == "YES" || s == "Yes" || s == "yes")
					{ content = true; return; }

				if (s == "OFF" || s == "Off" || s == "off")
					{ content = false; return; }
			}
			if (length == 4)
			{
				string s = "" + chars [0] + chars [1] + chars [2] + chars [3];

				if (s == "TRUE" || s == "True" || s == "true")
					{ content = true; return; }
			}
			if (length == 5)
			{
				string s = "" + chars [0] + chars [1] + chars [2] + chars [3] + chars[4];

				if (s == "FALSE" || s == "False" || s == "false")
					{ content = false; return; }
			}

			// No boolean
			throw new Exception ("No valid boolean");
		}
		
		/// <summary> Node content </summary>
		public bool Content
		{
			get { return content;  }
			set { content = value; }
		}
		
		/// <summary> To String </summary>
		public override string ToString ()
		{
			return "[BOOLEAN]" + content.ToString () + "[/BOOLEAN]";
		}

		/// <summary> Write to YAML </summary>
		protected internal override void Write (WriteStream stream)
		{
			if (Content)
				stream.Append ("y");
			else
				stream.Append ("n");
		}

	}
}
