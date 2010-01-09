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
	///   Class for storing a Yaml Integer node
	///   uri: tag:yaml.org,2002:int
	/// </summary>
	public class Integer : Scalar
	{
		private long content;
		
		/// <summary> New Integer </summary>
		public Integer (long val) :
				base ("tag:yaml.org,2002:int", NodeType.Integer)
		{
			content = val;
		}

		/// <summary> Content </summary>
		public long Content
		{
			get { return content; }
			set { content = value; }
		}
		
		/// <summary> Parse an integer </summary>
		public Integer (ParseStream stream) :
				base ("tag:yaml.org,2002:int", NodeType.Integer)
		{
			short sign   = 1; // Positive sign by default

			// Negative sign
			if (stream.Char == '-') 
			{
				sign = -1;
				stream.Next ();
			}
			// Positive sign
			else if (stream.Char == '+')
				stream.Next ();

			try
			{
				// Determine base
				if (stream.Char == '0')
				{
					stream.Next ();

					// Base 2
					if (stream.Char == 'b')
					{
						stream.Next ();
						content = ParseBase (stream, 2, sign);
						return;
					}
					// Base 16
					else if (stream.Char == 'x')
					{
						stream.Next ();
						content = Parse16 (stream, sign);
						return;
					}
					// Base 8
					else 
					{
						content = ParseBase (stream, 8, sign);
						return;
					}
				}

				// Other base
				stream.BuildLookaheadBuffer ();

				// First, try to parse with base 10
				try
				{
					content = ParseBase (stream, 10, sign);
					stream.DestroyLookaheadBuffer ();
					return;
				} 
				catch { }

				// If not parseable with base 10, then try base 60
				stream.RewindLookaheadBuffer ();
				stream.DestroyLookaheadBuffer ();

				content = Parse60 (stream, sign);
			}
			catch (Exception ex)
			{
				throw new ParseException (stream, ex.ToString ());
			}
		}

		/// <summary> Hexadecimal string </summary>
		private static long Parse16 (ParseStream stream, short sign)
		{
			uint output = 0;
			
			while (! stream.EOF)
			{
				// 0 .. 9
				if (stream.Char >= '0' && stream.Char <= '9')
				{
					output = (output * 16) + (uint) (stream.Char - '0');
					OverflowTest (output, sign);
				}
				// a .. f
				else if (stream.Char >= 'a' && stream.Char <= 'f')
				{
					output = (output * 16) + (uint) (stream.Char - 'a') + 10;
					OverflowTest (output, sign);
				}
				// A .. F
				else if (stream.Char >= 'A' && stream.Char <= 'F')
				{
					output = (output * 16) + (uint) (stream.Char - 'A') + 10;
					OverflowTest(output, sign);
				}
				// Ignore underscores, other chars are not allowed
				else if (stream.Char != '_')
					throw new Exception ("Unknown char in base 16");

				stream.Next ();
			}

			return (long) (sign * output);
		}

		/// <summary> Parses a string with a given base (maximum 10) </summary>
		/// <remarks>
		///   This is not completly correct. For base10 the first char may not be a '_'
		///   The other bases allow this...
		/// </remarks>
		private static long ParseBase (ParseStream stream, uint basis, short sign)
		{			
			// Base must be <= 10
			if (basis > 10)
				throw new Exception ("Base to large. Maximum 10");

			ulong output = 0;
			char max = (char) ((basis - 1) + (int) '0');

			// Parse
			while (! stream.EOF)
			{
				// Decimal
				if (stream.Char >= '0' && stream.Char <= max)
				{
					output = (output * basis) + (uint) (stream.Char - '0');
					OverflowTest (output, sign);
				}
				// Ignore underscores, but other chars are not allowed
				// see remarks
				else if (stream.Char != '_')
					throw new Exception ("Unknown char in base " + basis);

				stream.Next ();
			}
			return sign * (long) output;
		}

		/// <summary> Parses a string with base 60, without sign </summary>
		private static long Parse60 (ParseStream stream, short sign)
		{
			ulong output = 0;
			
			// Parse
			ulong part     = 0;
			bool firstPart = true; // Only the first part can be larger then 59

			while (! stream.EOF)
			{
				// Decimal
				if (stream.Char >= '0' && stream.Char <= '9')
					part = (part * 10) + (uint) (stream.Char - '0');

				// New part
				else if (stream.Char == ':')
				{
					// Only the first part can be largen then 60
					if ( ! firstPart)
						if (part >= 60)
							throw new
				Exception ("Part of base 60 scalar is too large (max. 59)");
					else
						firstPart = false;

					output = (output * 60) + part;
					OverflowTest(output, sign);
					part = 0;
				}

				// Ignore underscores, other chars are not allowed
				else if (stream.Char != '_')
					throw new Exception ("Unknown char in base 16");

				stream.Next ();
			}

			// Add last part to the output
			if (!firstPart)
				if (part >= 60)
					throw new Exception (
						"Part of base 60 scalar is too large (max. 59)");
			else
				firstPart = false;

			output = (output * 60) + part;
			OverflowTest (output, sign);

			return sign * (long) output;
		}

		/// <summary> Test that the unsigned int fits in a signed int </summary>
		/// <param name="number"> Value to test </param>
		/// <param name="sign"> Sign of the int where it must fit in </param>
		private static void OverflowTest (ulong number, short sign)
		{
			// NOTE: Negatif numbers can be one larger
			if ((sign >= 0 && number > System.Int64.MaxValue) ||
					(sign < 0 && number > (ulong) System.Int64.MaxValue + 1) )

				throw new Exception ("YAML overflow exception");
		}

		/// <summary> To String </summary>
		public override string ToString ()
		{
			return "[INTEGER]" + content.ToString () + "[/INTEGER]";
		}

		/// <summary> Write to YAML </summary>
		protected internal override void Write (WriteStream stream)
		{
			stream.Append (content.ToString ());
		}
	}
}
