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
	///   Class for storing a Yaml Null node
	///   tag:yaml.org,2002:null
	/// </summary>
	public class Null : Scalar
	{
		/// <summary> Null Constructor </summary>
		public Null () : base ("tag:yaml.org,2002:null", NodeType.Null) { }

		/// <summary> Parse a null node </summary>
		public Null (ParseStream stream) :
				base ("tag:yaml.org,2002:null", NodeType.Null)
		{
			// An empty string is a valid null node
			if (stream.EOF)
				return;

			else
			{
				// Read the first 4 chars
				char [] chars = new char [8];
				int length = 0;
				for (int i = 0; i < chars.Length && ! stream.EOF; i ++)
				{
					chars [i] = stream.Char;
					length ++;
					stream.Next ();
				}

				// Compare
				if (length == 1)
				{
					string s = "" + chars [0];

					// Canonical notation
					if (s == "~")
						return;
				}
				if (length == 4)
				{
					string s = "" + chars [0] + chars [1] + chars [2] + chars [3];

					// null, Null, NULL
					if (s == "NULL" || s == "Null" || s == "null")
						return;
				}

				throw new ParseException (stream, "Not NULL");
			}
		}

		/// <summary> Content property </summary>
		public object Content
		{
			get { return null; }
		}

		/// <summary> To String </summary>
		public override string ToString ()
		{
			return "[NULL]~[/NULL]";
		}

		/// <summary> Write to YAML </summary>
		protected internal override void Write (WriteStream stream)
		{
			stream.Append ("~");
		}
	}
}
