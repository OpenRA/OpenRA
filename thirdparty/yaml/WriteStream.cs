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

#define ENABLE_COMPRESSION

using System;
using System.Collections;

using System.IO;

namespace Yaml
{
	/// <summary>
	///   Help class for writing a Yaml tree to a string
	/// </summary>
	public class WriteStream
	{
		private TextWriter stream;
		private int        indentation       = 0;
		private bool       lastcharisnewline = false;

		private static string indentationChars = "    ";

		/// <summary> Constructor </summary>
		public WriteStream (TextWriter stream)
		{
			this.stream = stream;
		}

		/// <summary> Append a string </summary>
		public void Append (string s)
		{
			// Just add the text to the output stream when
			// there is no indentation
			if (indentation == 0)
				stream.Write (s);

			// Otherwise process each individual char
			else 
				for (int i = 0; i < s.Length; i ++)
				{
					// Indent after a newline
					if (lastcharisnewline)
					{
						WriteIndentation ();
						lastcharisnewline = false;
					}

					// Add char
					stream.Write (s [i]);

					// Remember newlines
					if (s [i] == '\n')
						lastcharisnewline = true;
				}
		}

		/// <summary> Indentation </summary>
		public void Indent ()
		{
			// Increase indentation level
			indentation ++;

			// Add a newline
#if ENABLE_COMPRESSION
			lastcharisnewline = false;
#else
			stream.Write ("\n");
			lastcharisnewline = true;
#endif
		}

		/// <summary> Write the indentation to the output stream </summary>
		private void WriteIndentation ()
		{
			for (int i = 0; i < indentation; i ++)
				stream.Write (indentationChars);
		}

		/// <summary> Unindent </summary>
		public void UnIndent ()
		{
			if (indentation > 0)
			{
				// Decrease indentation level
				indentation --;

				// Add a newline
				if (! lastcharisnewline)
					stream.Write ("\n");
				lastcharisnewline = true;
			}
			else
				throw new Exception ("Cannot unindent a not indented writestream.");

		}
	}
}
