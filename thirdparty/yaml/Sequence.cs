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
using System.Collections;

namespace Yaml
{
	/// <summary>
	///   Represents a Yaml Sequence
	/// </summary>
	public class Sequence : Node
	{
		private ArrayList childNodes = new ArrayList ();

		/// <summary> New, empty sequence </summary>
		public Sequence ( ) : base ("tag:yaml.org,2002:seq", NodeType.Sequence) { }

		/// <summary> New sequence from a node array </summary>
		public Sequence (Node [] nodes) :
				base ("tag:yaml.org,2002:seq", NodeType.Sequence)
		{
			foreach (Node node in nodes)
				childNodes.Add (node);
		}

		/// <summary> Parse a sequence </summary>
		public Sequence (ParseStream stream) :
				base ("tag:yaml.org,2002:seq", NodeType.Sequence)
		{
			// Is this really a sequence?
			if (stream.Char == '-')
			{
				// Parse recursively
				do {
					// Override the parent's stop chars, never stop
					stream.StopAt (new char [] { } );

					// Skip over '-'
					stream.Next ();

					// Parse recursively
					stream.Indent ();
					AddNode (Parse (stream));
					stream.UnIndent ();

					// Re-accept the parent's stop chars
					stream.DontStop ();
				}
				while ( ! stream.EOF && stream.Char == '-' );
			}
			// Or inline Sequence
			else if (stream.Char == '[')
			{
				// Override the parent's stop chars, never stop
				stream.StopAt (new char [] { });

				// Skip '['
				stream.Next ();

				do {
					stream.StopAt (new char [] {']', ','});
					stream.Indent ();
					AddNode (Parse (stream, false));
					stream.UnIndent ();
					stream.DontStop ();

					// Skip ','
					if (stream.Char != ']' && stream.Char != ',')
					{
						stream.DontStop ();
						throw new ParseException (stream, "Comma expected in inline sequence");
					}

					if (stream.Char == ',')
					{
						stream.Next ();
						stream.SkipSpaces ();
					}
				}
				while ( ! stream.EOF && stream.Char != ']');

				// Re-accept the parent's stop chars
				stream.DontStop ();

				// Skip ']'
				if (stream.Char == ']')
					stream.Next (true);
				else
					throw new ParseException (stream, "Inline sequence not closed");

			}
			// Throw an exception when not
			else
				throw new Exception ("This is not a sequence");
		}

		/// <summary> Add a node to this sequence </summary>
		public void AddNode (Node node)
		{
			if (node != null)
				childNodes.Add (node);
			else
				childNodes.Add (new Null ());
		}

		/// <summary> Get a node </summary>
		public Node this [int index]
		{
			get
			{
				if (index > 0 && index < childNodes.Count)
					return (Node) childNodes [index];

				else
					throw new IndexOutOfRangeException ();
			}
		}

		/// <summary> The node array </summary>
		public Node [] Nodes
		{
			get
			{
				Node [] nodes = new Node [childNodes.Count];

				for (int i = 0; i < childNodes.Count; i ++)
					nodes [i] = (Node) childNodes [i];

				return nodes;
			}
		}

		/// <summary> Textual destription of this node </summary>
		public override string ToString ()
		{
			string result = ""; 
			foreach (Node node in childNodes)
				result += node.ToString ();

			return "[SEQUENCE]" + result + "[/SEQUENCE]";
		}

		/// <summary> Node info </summary>
		public override Node Info ()
		{
			Mapping mapping = new Mapping ();
			mapping.AddMappingNode (new String ("kind"), new String ("sequence"));
			mapping.AddMappingNode (new String ("type_id"), new String (URI));

			Sequence childs = new Sequence ();

			foreach (Node child in childNodes)
				childs.AddNode (child.Info ());

			mapping.AddMappingNode (new String ("value"), childs);
			return mapping;
		}

		/// <summary> Write back to a stream </summary>
		protected internal override void Write (WriteStream stream)
		{
			foreach (Node node in childNodes)
			{
				stream.Append ("- ");

				stream.Indent ();
				node.Write (stream);
				stream.UnIndent ();
			}
		}

	}
}
