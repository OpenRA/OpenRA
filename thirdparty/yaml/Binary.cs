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
	///   A Yaml Boolean node
	///   tag:yaml.org,2002:binary
	/// </summary>
	public class Binary : Scalar
	{
		private byte [] content;

		/// <summary> Binary constructor from byte array </summary>
		/// <remarks> This constructor only sets the reference, no new memory is allocated </remarks>
		public Binary (byte[] val) : 
			base ("tag:yaml.org,2002:binary", NodeType.Binary)
		{
			content = val;
		}

		/// <summary> Parse a binary node </summary>
		public Binary (ParseStream stream) :
				base ("tag:yaml.org,2002:binary", NodeType.Binary)
		{
			try
			{
				content = Parse (stream);
			}
			catch (FormatException e)
			{
				throw new ParseException (stream, e);
			}
		}

		/// <summary> Binary content </summary>
		/// <remarks> There is no new memory allocated in the 'set'. </remarks>
		public byte [] Content
		{
			get { return content; }
			set { content = value; }
		}

		/// <summary> Parses a binairy node. </summary>
		/// <remarks>
		///   This is not an efficient method. First the stream is placed
		///   in a string. And after that the string is converted in a byte[].
		///   If there is a fault in the binairy string then that will only be detected
		///   after reading the whole stream and after coneverting.
		/// </remarks>
		public static new byte [] Parse (ParseStream stream)
		{
			bool quoted = false;
			bool block = false;
			System.Text.StringBuilder input = new System.Text.StringBuilder();

			if (stream.EOF)
				throw new ParseException (stream, "Empty node");

			// Detect block scalar
			stream.SkipSpaces ();
			if (stream.Char == '|')
			{
				block = true;
				stream.Next ();
				stream.SkipSpaces ();
			}

			while ( ! stream.EOF)
			{
				// Detect quotes
				if (stream.Char == '\"')
					if (quoted)
						break; //End of stream
					else
						quoted = true; //Start of quoted stream
				// Detect and ignore newline char's
				else if (!(stream.Char == '\n' && block))
					input.Append( stream.Char );

				stream.Next ();
			}

			//Console.WriteLine("convert [" + input.ToString() + "]");

			return System.Convert.FromBase64String (input.ToString ());
		}

		/// <summary> To String </summary>
		/// <remarks> The hexadecimal notation, 20 bytes for each line </remarks>
		public override string ToString ()
		{
			System.Text.StringBuilder output = new System.Text.StringBuilder ();
			
			output.Append ("[BINARY]\n\t");
			for (uint i = 0; i < content.Length; i++)
			{
				if ((i%16) == 0) 
					output.Append( "\n\t" );
				output.AppendFormat ("{0:X2} ", content[i]);
			}
			output.Append ("\n[/BINARY]");

			return output.ToString ();
		}

		/// <summary>
		///   Write the base64 content to YAML
		/// </summary>
		/// <remarks> The lines are splitted in blocks of 20 bytes </remarks>
		protected internal override void Write (WriteStream stream)
		{
			stream.Append("!!binary |" + "\n" );
			
			string bin = System.Convert.ToBase64String(content);

			while (bin.Length > 75)
			{
				stream.Append("  " + bin.Substring(0, 75) + "\n");
				bin = bin.Substring(75);
			}
			stream.Append("  " + bin );

			// Old coden, everything on one line
			// stream.Append ("!!binary \"" + System.Convert.ToBase64String (content) + "\"");
		}
	}

}
