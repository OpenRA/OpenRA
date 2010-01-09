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

// Unicode support:
// http://www.yoda.arachsys.com/csharp/unicode.html

namespace Yaml
{
	/// <summary>
	///   Yaml String node
	/// </summary>
	public class String : Scalar
	{
		private string content;
		private bool block  = false;
		private bool folded = false;
		
		/// <summary> New string constructor </summary>
		public String (string val) :
				base ("tag:yaml.org,2002:str", NodeType.String)
		{	
			content = val;
		}

		/// <summary> Parse a string </summary>
		public String (ParseStream stream) :
				base ("tag:yaml.org,2002:str", NodeType.String)
		{
			// set flags for folded or block scalar
			if (stream.Char == '>')  // TODO: '+' and '-' chomp chars
				folded = true;

			else if (stream.Char == '|')
				block = true;

			if (block || folded)
			{
				stream.Next ();
				stream.SkipSpaces ();
			}

			// -----------------
			// Folded Scalar
			// -----------------
			if (folded)
			{
				System.Text.StringBuilder builder = new System.Text.StringBuilder ();

				// First line (the \n after the first line is always ignored,
				// not replaced with a whitespace)
				while (! stream.EOF && stream.Char != '\n')
				{
					builder.Append (stream.Char);
					stream.Next ();
				}

				// Skip the first newline
				stream.Next ();

				// Next lines (newlines will be replaced by spaces in folded scalars)
				while (! stream.EOF)
				{
					if (stream.Char == '\n')
						builder.Append (' ');
					else
						builder.Append (stream.Char);

					stream.Next (true);
				}
				content = builder.ToString ();
			}

			// -----------------
			// Block Scalar (verbatim block without folding)
			// -----------------
			else if (block)
			{
/*
Console.Write(">>");
while (! stream.EOF)
{
	Console.Write (stream.Char);
	stream.Next();
}
Console.Write("<<");
// */

				System.Text.StringBuilder builder = new System.Text.StringBuilder ();
				while (! stream.EOF)
				{
					builder.Append (stream.Char);
					stream.Next (true);
				}
				content = builder.ToString ();
			}

			// String between double quotes
			if (stream.Char == '\"')
				content = ParseDoubleQuoted (stream);

			// Single quoted string
			else if (stream.Char == '\'')
				content = ParseSingleQuoted (stream);

			// String without quotes
			else
				content = ParseUnQuoted (stream);
		}

		/// <summary>
		///   Parses a String surrounded with single quotes
		/// </summary>
		private string ParseSingleQuoted (ParseStream stream)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();
			
			// Start literal parsing
			stream.StartLiteral ();

			// Skip '''
			stream.Next (true);
			
			while (! stream.EOF)
			{
				if (stream.Char == '\'')
				{
					stream.Next ();

					// Escaped single quote
					if (stream.Char == '\'')
						builder.Append (stream.Char);

						// End of string
					else
						break;
				}
				else
					builder.Append (stream.Char);

				stream.Next ();

				// Skip \'
				if (stream.EOF)
				{
					stream.StopLiteral ();
					throw new ParseException (stream,
						"Single quoted string not closed");
				}
			}

			// Stop literal parsing
			stream.StopLiteral ();

			return builder.ToString();
		}

		/// <summary>
		///   Parses a String surrounded with double quotes
		/// </summary>
		private string ParseDoubleQuoted(ParseStream stream)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();
			
			// Skip '"'
			stream.Next ();

			// Stop at "
			stream.StopAt (new char [] {'\"'} );

			while (! stream.EOF)
			{
				if (stream.Char == '\n')
				{
					builder.Append (' ');
					stream.Next ();
				}
				else
					builder.Append (NextUnescapedChar (stream));
			}

			// Don't stop at "
			stream.DontStop ();

			// Skip '"'
			if (stream.Char != '\"')
				throw new ParseException (stream,
					"Double quoted string not closed");
			else
				stream.Next (true);

			return builder.ToString();
		}

		/// <summary>
		///   Parses a String surrounded without nothing
		/// </summary>
		private string ParseUnQuoted(ParseStream stream)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();

			while (! stream.EOF)
				builder.Append (NextUnescapedChar (stream));

			// Trimming left
			int count = 0;
			while (count < builder.Length &&
				(builder [count] == ' ' || builder [count] == '\t'))
				count ++;

			if (count >= 0)
				builder.Remove (0, count);
	
			// Trimming right
			count = 0;
			while (count < builder.Length &&
				(builder [builder.Length - count - 1] == ' ' ||
				builder [builder.Length - count - 1] == '\t'))
				count ++;
				
			if (count >= 0)
				builder.Remove (builder.Length - count, count);

			return builder.ToString();
		}

		/// <summary>
		///   Reads a character from the stream, unescapes it,
		///   and moves to the next character.
		/// </summary>
		private char NextUnescapedChar (ParseStream stream)
		{
			char c = stream.Char;

			// If escaped
			if (c == '\\')
			{
				// Never stop, every special character
				// looses its meaning behind a backslash.
				stream.StopAt (new Char [] { });

				stream.Next (true);
				c = stream.Char;

				// ASCII null
				if (c == '0')          c = '\0';

				// ASCII bell
				else if (c == 'a')     c = (char) 0x7;

				// ASCII backspace
				else if (c == 'b')     c = (char) 0x8;
				
				// ASCII horizontal tab
				else if (c == 't')     c = (char) 0x9;

				// ASCII newline
				else if (c == 'n')     c = (char) 0xA;

				// ASCII vertical tab
				else if (c == 'v')     c = (char) 0xB;

				// ASCII form feed
				else if (c == 'f')     c = (char) 0xC;

				// ASCII carriage return
				else if (c == 'r')     c = (char) 0xD;

				// ASCII escape
				else if (c == 'e')     c = (char) 0x1D;

				// Unicode next line
				else if (c == 'N')     c = (char) 0x85;

				// Unicode non breaking space
				else if (c == '_')     c = (char) 0xA0;

				// TODO larger unicode characters

				// Unicode line separator
				// else if (c == 'L')     c = (char) 0x20282028;

				// 8 bit hexadecimal
				else if (c == 'x')
				{
					int c_int = (char) 0;

					for (int i = 0; i < 2; i ++)
					{
						c_int *= 16;

						stream.Next ();
						char d = stream.Char;

						if (d >= '0' && d <= '9')
							c_int += d - '0';

						else if (d >= 'a' && d <= 'f')
							c_int += d - 'a';

						else if (d >= 'A' && d <= 'F')
							c_int += d - 'A';
						else
						{
							stream.DontStop ();
							throw new ParseException (stream,
								"Invalid escape sequence");
						}
					}
					c = (char) c_int;
				}

				stream.Next (true);

				// Restore last stop settings
				stream.DontStop ();
			}
			else
				stream.Next (true);

			return c;
		}
		
		/// <summary> Content property </summary>
		public string Content
		{
			get { return content;  }
			set { content = value; }
		}
		
		/// <summary> To String </summary>
		public override string ToString ()
		{
			return "[STRING]" + content + "[/STRING]";
		}

		/// <summary> Write </summary>
		protected internal override void Write (WriteStream stream)
		{
			// TODO, not required, but writing to block or folded scalars
			//       generates a little more neat code.

			// Analyze string
			bool multiline    = false;
			bool mustbequoted = false;

			for (int i = 0; i < content.Length; i ++)
			{
				char c = content [i];

				if (c == '\n')
					multiline = true;

				// We quote everything except strings like /[a-zA-Z]*/
				// However there are more strings which don't require
				// quotes.
				if ( ! ( c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z'))
					mustbequoted = true;
			}

			// Double quoted strings
			if (mustbequoted)
			{
				stream.Append ("\"");

				for (int i = 0; i < content.Length; i ++)
				{
					char c = content [i];

					// Backslash
					if (c == '\\')             stream.Append ("\\" + "\\");

					// Double quote
					else if (c == '\"')        stream.Append ("\\" + "\"");

					// Single quote
					else if (c == '\'')        stream.Append ("\\" + "\'");

					// ASCII null
					else if (c == '\0')        stream.Append ("\\0");

					// ASCII bell
					else if (c == (char) 0x7)  stream.Append ("\\a");

					// ASCII backspace
					else if (c == (char) 0x8)  stream.Append ("\\b");
					
					// ASCII horizontal tab
					else if (c == (char) 0x9)  stream.Append ("\\t");

					// ASCII newline
					else if (c == (char) 0xA)  stream.Append ("\\n");

					// ASCII vertical tab
					else if (c == (char) 0xB)  stream.Append ("\\v");

					// ASCII form feed
					else if (c == (char) 0xC)  stream.Append ("\\f");

					// ASCII carriage return
					else if (c == (char) 0xD)  stream.Append ("\\r");

					// ASCII escape
					else if (c == (char) 0x1D)  stream.Append ("\\e");

					// Unicode next line
					else if (c == (char) 0x85)  stream.Append ("\\N");

					// Unicode non breaking space
					else if (c == (char) 0xA0)  stream.Append ("\\_");

					// TODO larger unicode characters

					else
						stream.Append ("" + c);
				}
				stream.Append ("\"");
			}

			// Simple non-quoted strings
			else
				stream.Append (content);
		}
	}
}
