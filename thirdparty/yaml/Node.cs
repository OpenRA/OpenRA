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

#define UNSTABLE
#define SUPPORT_EXPLICIT_TYPES
#define SUPPORT_IMPLICIT_MAPPINGS

using System;
using System.Text;
using System.Collections;

using System.IO;

namespace Yaml
{
	/// <summary>
	///   Kind of node, used to determine the type of node.
	/// </summary>
	public enum NodeType
	{
		/// <summary>A Yaml mapping - collection type</summary>
		Mapping,
		/// <summary>A Yaml sequence - collection type</summary>
		Sequence,

		/// <summary>A Yaml binary scalar </summary>
		Binary,
		/// <summary>A Yaml boolean scalar </summary>
		Boolean,
		/// <summary>A Yaml float scalar </summary>
		Float,
		/// <summary>A Yaml integer scalar </summary>
		Integer,
		/// <summary>A Yaml null scalar </summary>
		Null,
		/// <summary>A Yaml string scalar </summary>
		String,
		/// <summary>A Yaml timestamp scalar </summary>
		Timestamp
	};

	/// <summary>
	///   Node in the Yaml tree
	/// </summary>

	public abstract class Node
	{
		/// <summary> The uri given by http://yaml.org/type/ </summary>
		protected readonly string uri;

		/// <summary> Determines wich node we are talking about </summary>
		protected NodeType nodetype;

		/// <summary> Node Constructor </summary>
		/// <param name="uri"> URI of the node </param>
		/// <param name="nodetype"> The type of node that we want to store </param>
		public Node (string uri, NodeType nodetype)
		{
			this.uri      = uri;
			this.nodetype = nodetype;
		}
		
		/// <summary> Parse a Yaml string and return a Yaml tree </summary>
		public static Node Parse (string lines)
		{
			StringReader reader = new StringReader (lines);
			Node node = Parse (new ParseStream (reader));
			reader.Close ();
			return node;
		}

		/// <summary> Parse a Yaml string from a textreader and return a Yaml tree </summary>
		public static Node Parse (TextReader textreader)
		{
			return Parse (new ParseStream (textreader));
		}

		/// <summary> Return a Yaml string </summary>
		public string Write ()
		{
			StringWriter stringWriter = new StringWriter ();
			WriteStream writeStream = new WriteStream (stringWriter);

			Write (writeStream);

			stringWriter.Close ();
			return stringWriter.ToString ();
		}

		/// <summary>
		///   Parse a Yaml string from a textfile and return a Yaml tree
		/// </summary>
		public static Node FromFile (string filename)
		{
			// Open YAML file
			StreamReader reader = File.OpenText (filename);
			ParseStream parsestream = new ParseStream (reader);

			// Parse
			Node node = Parse (parsestream);

			// Close YAML file
			reader.Close ();
			return node;
		}

		/// <summary>
		///   Write a YAML tree to a file using UTF-8 encoding
		/// </summary>
		public void ToFile (string filename)
		{
			ToFile (filename, Encoding.UTF8);
		}

		/// <summary>
		///   Write a YAML tree to a file
		/// </summary>
		public void ToFile (string filename, Encoding enc)
		{
			// Open YAML file
			StreamWriter writer = new StreamWriter (filename, false, enc);
			WriteStream writestream = new WriteStream (writer);

			// Write
			Write (writestream);

			// Close YAML file
			writer.Close ();
		}

		/// <summary> Parse function </summary>
		protected static Node Parse (ParseStream stream) { return Parse (stream, true); }

		/// <summary> Internal parse method </summary>
		/// <param name="parseImplicitMappings">
		///   Avoids ethernal loops while parsing implicit mappings. Implicit mappings are
		///   not rocognized by a leading character. So while trying to parse the key of
		///   something we think that could be a mapping, we're sure that if it is a mapping,
		///   the key of this implicit mapping is not a mapping itself.
		///
		///   NOTE: Implicit mapping still belong to unstable code and require the UNSTABLE and
		///         IMPLICIT_MAPPINGS preprocessor flags.
		/// </param>
		/// <param name="stream"></param>
		protected static Node Parse (ParseStream stream, bool parseImplicitMappings)
		{
			// ----------------
			// Skip Whitespace
			// ----------------
			if (! stream.EOF)
			{
				// Move the firstindentation pointer after the whitespaces of this line
				stream.SkipSpaces ();
				while (stream.Char == '\n' && ! stream.EOF)
				{
					// Skip newline and next whitespaces
					stream.Next ();
					stream.SkipSpaces ();
				}
			}

			// -----------------
			// No remaining chars (Null/empty stream)
			// -----------------
			if (stream.EOF)
				return new Null ();

			// -----------------
			// Explicit type
			// -----------------

#if SUPPORT_EXPLICIT_TYPES
			stream.BuildLookaheadBuffer ();

			char a = '\0', b = '\0';

			a = stream.Char; stream.Next ();
			b = stream.Char; stream.Next ();

			// Starting with !!
			if (a == '!' && b == '!' && ! stream.EOF)
			{
				stream.DestroyLookaheadBuffer ();

				// Read the tagname
				string tag = "";

				while (stream.Char != ' ' && stream.Char != '\n' && ! stream.EOF)
				{
					tag += stream.Char;
					stream.Next ();
				}

				// Skip Whitespace
				if (! stream.EOF)
				{
					stream.SkipSpaces ();
					while (stream.Char == '\n' && ! stream.EOF)
					{
						stream.Next ();
						stream.SkipSpaces ();
					}
				}

				// Parse
				Node n;
				switch (tag)
				{
					// Mappings and sequences
					// NOTE:
					// - sets are mappings without values
					// - Ordered maps are ordered sequence of key: value
					//   pairs without duplicates.
					// - Pairs are ordered sequence of key: value pairs
					//   allowing duplicates.

					// TODO: Create new datatypes for omap and pairs
					//   derived from sequence with a extra duplicate
					//   checking.

					case "seq":       n = new Sequence  (stream); break;
					case "map":       n = new Mapping   (stream); break;
					case "set":       n = new Mapping   (stream); break;
					case "omap":      n = new Sequence  (stream); break;
					case "pairs":     n = new Sequence  (stream); break;

					// Scalars
					//
					// TODO: do we have to move this to Scalar.cs
					// in order to get the following working:
					//
					// !!str "...": "..."
					// !!str "...": "..."

					case "timestamp": n = new Timestamp (stream); break;
					case "binary":    n = new Binary    (stream); break;
					case "null":      n = new Null      (stream); break;
					case "float":     n = new Float     (stream); break;
					case "int":       n = new Integer   (stream); break;
					case "bool":      n = new Boolean   (stream); break;
					case "str":       n = new String    (stream); break;

					// Unknown data type
					default:
						throw  new Exception ("Incorrect tag '!!" + tag + "'");
				}

				return n;
			}
			else
			{
				stream.RewindLookaheadBuffer ();
				stream.DestroyLookaheadBuffer ();
			}
#endif
			// -----------------
			// Sequence
			// -----------------

			if (stream.Char == '-' || stream.Char == '[')
				return new Sequence (stream);

			// -----------------
			// Mapping
			// -----------------

			if (stream.Char == '?' || stream.Char == '{')
				return new Mapping (stream);

			// -----------------
			// Try implicit mapping
			// -----------------

			// This are mappings which are not preceded by a question
			// mark. The keys have to be scalars.

#if (UNSTABLE && SUPPORT_IMPLICIT_MAPPINGS)

			// NOTE: This code can't be included in Mapping.cs
			// because of the way we are using to rewind the buffer.

			Node key, val;

			if (parseImplicitMappings)
			{
				// First Key/value pair

				stream.BuildLookaheadBuffer ();

				stream.StopAt (new char [] {':'});

				// Keys of implicit mappings can't be sequences, or other mappings
				// just look for scalars
				key = Scalar.Parse (stream, false); 
				stream.DontStop ();

Console.WriteLine ("key: " + key);

				// Followed by a colon, so this is a real mapping
				if (stream.Char == ':')
				{
					stream.DestroyLookaheadBuffer ();

					Mapping mapping = new Mapping ();

					// Skip colon and spaces
					stream.Next ();
					stream.SkipSpaces ();

					// Parse the value
Console.Write ("using  buffer: " + stream.UsingBuffer ());
					stream.Indent ();
Console.Write ("using  buffer: " + stream.UsingBuffer ());
//					val = Parse (stream, false);
Console.Write ("<<");
while (!stream.EOF) {Console.Write (stream.Char);stream.Next (true);}
Console.Write (">>");

val = new  String (stream);


Console.Write ("using  buffer: " + stream.UsingBuffer ());
					stream.UnIndent ();
Console.Write ("using  buffer: " + stream.UsingBuffer ());

Console.Write ("<<");
while (!stream.EOF) {Console.Write (stream.Char);stream.Next (true);}
Console.Write (">>");




Console.WriteLine ("val: " + val);
					mapping.AddMappingNode (key, val);

					// Skip possible newline
					// NOTE: this can't be done by the drop-newline
					// method since this is not the end of a block
					while (stream.Char == '\n')
						stream.Next (true);

					// Other key/value pairs
					while (! stream.EOF)
					{
						stream.StopAt (new char [] {':'} );
						stream.Indent ();
						key = Scalar.Parse (stream);
						stream.UnIndent ();
						stream.DontStop ();

Console.WriteLine ("key 2: " + key);
						if (stream.Char == ':')
						{
							// Skip colon and spaces
							stream.Next ();
							stream.SkipSpaces ();

							// Parse the value
							stream.Indent ();
							val = Parse (stream);
							stream.UnIndent ();

Console.WriteLine ("val 2: " + val);
							mapping.AddMappingNode (key, val);
						}
						else // TODO: Is this an error?
						{
							// NOTE: We can't recover from this error,
							// the last buffer has been destroyed, so
							// rewinding is impossible.
							throw new ParseException (stream,
								"Implicit mapping without value node");
						}

						// Skip possible newline
						while (stream.Char == '\n')
							stream.Next ();
					}

					return mapping;
				}

				stream.RewindLookaheadBuffer ();
				stream.DestroyLookaheadBuffer ();
			}

#endif
			// -----------------
			// No known data structure, assume this is a scalar
			// -----------------

			Scalar scalar = Scalar.Parse (stream);

			// Skip trash
			while (! stream.EOF)
				stream.Next ();
		

			return scalar;
		}

		/// <summary>
		///   URI of this node, according to the YAML documentation.
		/// </summary>
		public string URI
		{
			get { return uri; }
		}

		/// <summary>
		///   Kind of node: mapping, sequence, string, ...
		/// </summary>
		public NodeType Type
		{
			get { return nodetype; }
		}

		/// <summary>
		///   Writes a Yaml tree back to a file or stream  
		/// </summary>
		/// <remarks>
		///   should not be called from outside the parser. This method
		///   is only public from inside the Sequence and Mapping Write
		///   methods.
		/// </remarks>
		/// <param name="stream">Were the output data go's</param>
		protected internal virtual void Write (WriteStream stream) {}

		/// <summary>
		///   The ToString method here, and in all the classses
		///   derived from this class, is used mainly for debugging
		///   purpose. ToString returns a xml-like textual representation
		///   of the objects. It's very useful to see how a Yaml document
		///   has been parsed because of the disambiguous representation
		///   of this notation.
		/// </summary>
		public override abstract string ToString ();

		/// <summary>
		///   Node info returns a YAML node and is also mostly used
		///   for debugging the parser. This could be used for
		///   traversing the meta-info of another YAML tree
		/// </summary>
		public abstract Node Info ();
	}
}
