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
	///   Class for storing a Yaml Float node
	///   tag:yaml.org,2002:float
	/// </summary>
	public class Float : Scalar
	{
		private double content;
		
		/// <summary> New float </summary>
		public Float (float val) : base ("tag:yaml.org,2002:float", NodeType.Float)
		{
			content = val;
		}

		/// <summary> Parse float </summary>
		public Float (ParseStream stream) :
				base ("tag:yaml.org,2002:float", NodeType.Float)
		{
			// NaN
			if (stream.Char == '.')
			{
				stream.Next (true);
				ParseNaN (stream);
			}
			else
			{
				// By default positief
				int sign = 1;

				// Negative sign
				if (stream.Char == '-') 
				{
					sign = -1;
					stream.Next (true);
				}
				// Positive sign
				else if (stream.Char == '+')
					stream.Next (true);

				// Test for inf, Inf and INF
				if ( ! stream.EOF && stream.Char == '.')
				{
					stream.Next (true);
					ParseInf (stream, sign);
				}
				// Parse the numbers
				else if (!stream.EOF)
					ParseNumber (stream, sign);

				else
					throw new ParseException (stream,
						"No valid float, no data behind the sign");
			}
		}

		#region Parse special formats (NaN and Inf)
		/// <summary>
		///   Test for the value's nan, NaN and NAN in the stream. If
		///   found then it is placed inside the content.
		///   There is no more data excepted behind it
		/// </summary>
		private void ParseNaN (ParseStream stream)
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
			if (length == 3)
			{
				string s = "" + chars [0] + chars [1] + chars [2];

				if (s == "NAN" || s == "NaN" || s == "nan")
				{
					content = double.NaN;
					return;
				}
			}

			throw new ParseException (stream, "No valid NaN");
		}

		/// <summary>
		///   Test for the value's inf, Inf and INF in the stream. If
		///   found then it 'merged' with the sign and placed in the content.
		///   There is no more data excepted behind it.
		/// </summary>
		private void ParseInf (ParseStream stream, int sign)
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
			if (length == 3)
			{
				string s = "" + chars [0] + chars [1] + chars [2];

				if (s == "INF" || s == "Inf" || s == "inf")
				{
					if (sign < 0)
						content = double.NegativeInfinity;
					else
						content = double.PositiveInfinity;

					return;
				}
			}

			throw new ParseException (stream, "No valid infinity");
		}
		#endregion

		/// <summary>
		///   If it is not Infinity or NaN, then parse as a number
		/// </summary>
		private void ParseNumber (ParseStream stream, int sign)
		{
			bool base60       = false; // Base 60 with ':'
			bool afterDecimal = false; // Before or after the decimal point

			double factor = 0.1;
			double part; // If base60 different parts, else output value

			// Set sign
			content = sign >= 0 ? 1 : -1;

			// First char must 0-9
			if (stream.Char >= '0' && stream.Char <= '9')
			{
				part = (uint) (stream.Char - '0');
				stream.Next (true);
			}
			else
				throw new ParseException (stream,
					"No valid float: Invalid first character of float: " + stream.Char);

			while (! stream.EOF)
			{
				// Decimal
				if (stream.Char >= '0' && stream.Char <= '9')
					if (afterDecimal)
					{
						part += (uint) (stream.Char - '0') * factor;
						factor *= 0.1;
					}
					else
						part = (part * 10) + (uint) (stream.Char - '0');

				// Base60 detected
				else if (stream.Char == ':')
				{
					if ( ! base60) // First time
					{
						content *= part; // Multiply to get sign
						part = 0;
						base60 = true; // We are now sure base 60
					}
					else
					{
						if (part > 59)
							throw new ParseException (stream,
								"Part of base 60 can't be larger then 59");
						content = (60 * content) + part;
						part = 0;
					}
				}
				// If first '.', then after decimal, else it is ignored if not in Base60
				else if ( (!base60 || (base60 && !afterDecimal)) && stream.Char == '.' )
					afterDecimal = true;

				// Determine scientific notation
				else if ( (stream.Char == 'E' || stream.Char == 'e') && ! base60 )
				{
					stream.Next (true);
					content *= Math.Pow (10, ParseScient (stream));
				}
				// Ignore underscores if before the decimal point, special case if base 60
				else if ((!afterDecimal || (afterDecimal && base60)) && stream.Char != '_')
					throw new ParseException (stream, "Unknown char");

				stream.Next (true);
			}

			// Add last part of base to content
			if (base60)
				content = (60 * content) + part;
			else
				content *= part; // Multiply to get sign
		}

		/// <summary> Parses the exponential part of the float </summary>
		private static long ParseScient (ParseStream stream)
		{
			ulong output = 0;
			short sign;

			if (stream.Char == '-')
				sign = -1;
			else if (stream.Char == '+')
				sign = 1;
			else
				throw new ParseException (stream,
						"Excepted + or - for the exponential part");

			stream.Next (true);

			while (! stream.EOF)
			{
				if (stream.Char >= '0' && stream.Char <= '9')
					output = (10 * output) + (uint) stream.Char - '0';
				else
					throw new ParseException (stream,
							"Unexepted char in exponential part: >" +
							stream.Char + "<");

				stream.Next (true);
			}
            			
			return sign * (long) output;
		}

		/// <summary> Content </summary>
		public double Content
		{
			get { return content; }
			set { content = value; }
		}

		/// <summary> To string </summary>
		public override string ToString()
		{
			return "[FLOAT]" + content + "[/FLOAT]";
		}

		/// <summary> Write to a YAML node </summary>
		protected internal override void Write (WriteStream stream)
		{
			if (content.Equals( double.NaN ))
				stream.Append ("!!float .NaN");

			else if (content.Equals( double.NegativeInfinity ))
				stream.Append ("!!float -.Inf");
				
			else if (content.Equals( double.PositiveInfinity ))
				stream.Append ("!!float +.Inf");

			else stream.Append ("!!float " + content);
		}
	}
}
