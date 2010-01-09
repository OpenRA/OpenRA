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


// TODO Access to nodes via the [] overload

namespace Yaml
{
	/// <summary>
	///   Yaml Mapping
	/// </summary>
	public class Mapping : Node
	{
		private ArrayList childNodes = new ArrayList ();

		/// <summary> New empty mapping </summary>
		public Mapping () : base ("tag:yaml.org,2002:map", NodeType.Mapping) { }

		/// <summary> New mapping from a mappingnode array </summary>
		public Mapping (MappingNode [] nodes) :
				base ("tag:yaml.org,2002:map", NodeType.Mapping)
		{
			foreach (MappingNode node in nodes)
				childNodes.Add (node);
		}

		/// <summary> Parse a mapping </summary>
		public Mapping (ParseStream stream) :
				base ("tag:yaml.org,2002:map", NodeType.Mapping)
		{
			// Mapping with eplicit key, (implicit mappings are threaded
			// in Node.cs)
			if (stream.Char == '?')
			{
				// Parse recursively
				do {
					Node key, val;

					// Skip over '?'
					stream.Next ();
					stream.SkipSpaces ();

					// Parse recursively. The false param avoids
					// looking recursively for implicit mappings.
					stream.StopAt (new char [] {':'});
					stream.Indent ();
					key = Parse (stream, false);
					stream.UnIndent ();
					stream.DontStop ();

					// Parse recursively. The false param avoids
					// looking for implit nodes
					if (stream.Char == ':')
					{
						// Skip over ':'
						stream.Next ();
						stream.SkipSpaces ();

						// Parse recursively
						stream.Indent ();
						val = Parse (stream);
						stream.UnIndent ();
					}
					else
						val = new Null ();

					AddMappingNode (key, val);

					// Skip possible newline
					// NOTE: this can't be done by the drop-newline
					// method since this is not the end of a block
					if (stream.Char == '\n')
						stream.Next ();
				}
				while ( ! stream.EOF && stream.Char == '?');
			}
			// Inline mapping
			else if (stream.Char == '{')
			{
				// Override the parent's stop chars, never stop
				stream.StopAt (new char [] { });

				// Skip '{'
				stream.Next ();

				do {
					Node key, val;

					// Skip '?'
					// (NOTE: it's not obligated to use this '?',
					// especially because this is an inline mapping)
					if (stream.Char == '?')
					{
						stream.Next ();
						stream.SkipSpaces ();
					}

					// Parse recursively the key
					stream.StopAt (new char [] {':', ',', '}'});
					stream.Indent ();
						key = Parse (stream, false);
					stream.UnIndent ();
					stream.DontStop ();

					// Value
					if (stream.Char == ':')
					{
						// Skip colon
						stream.Next ();
						stream.SkipSpaces ();

						// Parse recursively the value
						stream.StopAt (new char [] {'}', ','});
						stream.Indent ();
							val = Parse (stream, false);
						stream.UnIndent ();
						stream.DontStop ();
					}
					else
						val = new Null ();

					AddMappingNode (key, val);

					// Skip comma (key sepatator)
					if (stream.Char != '}' && stream.Char != ',')
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
				while ( ! stream.EOF && stream.Char != '}' );

				// Re-accept the parent's stop chars
				stream.DontStop ();

				// Skip '}'
				if (stream.Char == '}')
					stream.Next ();
				else
					throw new ParseException (stream, "Inline mapping not closed");
			}
		}

		/// <summary> Add a node to this mapping </summary>
		public void AddMappingNode (Node key, Node val)
		{
			childNodes.Add (new MappingNode (key, val));
		}

		/// <summary> Add a node to this mapping </summary>
		public void AddMappingNode (MappingNode node)
		{
			if (node != null)
				childNodes.Add (node);
			else
				childNodes.Add (new MappingNode (null, null));
		}

		/// <summary> Number of mappings </summary>
		public int Count
		{
			get { return childNodes.Count; }
		}

		/// <summary> To String </summary>
		public override string ToString ()
		{
			string result = ""; 
			foreach (MappingNode node in childNodes)
				result += node.ToString ();
			
			return "[MAPPING]" + result + "[/MAPPING]";
		}
		
		/// <summary> Node info </summary>
		public override Node Info ()
		{
			Mapping mapping = new Mapping ();
			mapping.AddMappingNode (new String ("kind"), new String ("mapping"));
			mapping.AddMappingNode (new String ("type_id"), new String (URI));

			Mapping childs = new Mapping ();
			int i = 0;
			foreach (MappingNode child in childNodes)
			{
				Sequence keyvaluepair = new Sequence ();
				keyvaluepair.AddNode (child.Key.Info () );
				keyvaluepair.AddNode (child.Value.Info ());

				childs.AddMappingNode (new String ("key_" + i), keyvaluepair);
				i ++;
			}

			mapping.AddMappingNode (new String ("value"), childs);
			return mapping;
		}

		/// <summary> Write to YAML </summary>
		protected internal override void Write (WriteStream stream)
		{
			foreach (MappingNode node in childNodes)
			{
				stream.Append ("? ");

				stream.Indent ();
				Yaml.Node key = node.Key;
				key.Write (stream);
				stream.UnIndent ();

				stream.Append (": ");

				stream.Indent ();
				node.Value.Write (stream);
				stream.UnIndent ();
			}
		}

	}

	/// <summary>
	///   Node pair (key, value) of a mapping
	/// </summary>
	public class MappingNode
	{
		private Node key;
		private Node val;

		/// <summary> Create a new mappingnode </summary>
		public MappingNode (Node key, Node val)
		{
			if (key == null) key = new Null ();
			if (val == null) val = new Null ();

			this.key = key;
			this.val = val;
		}

		/// <summary> Key property </summary>
		public Node Key
		{
			get { return key; }
			set { key = (value == null ? new Null () : value); }
		}

		/// <summary> Value property </summary>
		public Node Value
		{
			get { return val; }
			set { val = (value == null ? new Null () : value); }
		}

		/// <summary> To String </summary>
		public override string ToString ()
		{
			return
				"[KEY]" + key.ToString () + "[/KEY]" +
				"[VAL]" + val.ToString () + "[/VAL]";
		}
	}
}
