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
	///   Yaml Timestamp node
	///   uri: tag:yaml.org,2002:timestamp
	/// </summary>
	public class Timestamp : Scalar
	{
		private System.DateTime content;

		/// <summary>
		///   Represents the offset from the UTC time in hours
		/// </summary>
		/// <remarks>
		///   We use this extra variable for compatibility with Mono
		///   and .NET 1.0. .NET 2.0 has an extra property for
		///   System.Datetime for the timezone.
		/// </remarks>
		private double timezone = 0;
		
		/// <summary>
		/// Basic constructor that takes a given datetime
		/// </summary>
		/// <param name="datetime">A .NET 1.0 datetime</param>
		public Timestamp (DateTime datetime) :
				base ("tag:yaml.org,2002:timestamp", NodeType.Timestamp)
		{
			this.content  = datetime;
			this.timezone = 0;
		}

		/// <summary>
		/// Basic constructor, that also gives the posibility to set a timezone
		/// </summary>
		/// <param name="datetime">A .NET 1.0 datetime</param>
		/// <param name="timezone">The offset, in hours,r to UTC that determine the timezone </param>
		public Timestamp (DateTime datetime, double timezone) :
				base ("tag:yaml.org,2002:timestamp", NodeType.Timestamp)
		{
			this.content  = datetime;
			this.timezone = timezone;
		}

		/// <summary> Content property </summary>
		public System.DateTime Content
		{
			get { return content; }
			set { content = value; }
		}

		/// <summary> Timezone, an offset in hours </summary>
		public double Timezone
		{
			get { return timezone; }
			set { timezone = value; }
		}
			
		/// <summary> To String </summary>
		public override string ToString ()
		{
			return "[TIMESTAMP]" + YamlString () + "[/TIMESTAMP]";
		}

		/// <summary> Parse a DateTime </summary>
		public Timestamp (ParseStream stream) :
				base ("tag:yaml.org,2002:timestamp", NodeType.Timestamp)
		{
			int year     = 0;
			int month    = 0;
			int day      = 0;
			int hour     = 0;
			int minutes  = 0;
			int seconds  = 0;
			int ms       = 0;

			try
			{
				// Parse year
				year = ParseNumber (stream, 4);
				SkipChar (stream, '-');			

				// Parse month
				month = ParseNumber (stream, 2);
				SkipChar (stream, '-');

				// Parse day
				day = ParseNumber (stream, 2);

				// Additional, the time
				if ( ! stream.EOF)
					ParseTime (stream, out hour, out minutes, out seconds);

				// Additional, milliseconds
				if ( ! stream.EOF)
					ms = ParseMilliSeconds (stream);

				// Additional, the timezone
				if ( ! stream.EOF)
					timezone = ParseTimezone (stream);

				// If there is more, then a format exception
				if ( ! stream.EOF)
					throw new Exception ("More data then excepted");

				content = new DateTime (year, month, day, hour, minutes, seconds, ms);
			}
			catch (Exception ex)
			{
				throw new ParseException (stream, ex.ToString ());
			}
		}

		/// <summary>
		///   Parse the time (hours, minutes, seconds)
		/// </summary>
		private void ParseTime (ParseStream stream,
				out int hour, out int minutes, out int seconds)
		{
			if (stream.Char == 't' || stream.Char == 'T')
				stream.Next (true);
			else
				SkipWhitespace (stream);

			// Parse hour
			// Note: A hour can be represented by one or two digits.
			string hulp = "";
			while (stream.Char >= '0' && stream.Char <= '9' &&
					! stream.EOF && hulp.Length <= 2)
			{
				hulp += stream.Char;
				stream.Next (true);
			}
			hour = Int32.Parse (hulp);

			SkipChar (stream, ':');

			// Parse minutes
			minutes = ParseNumber (stream, 2);
			SkipChar (stream, ':');

			// Parse seconds
			seconds = ParseNumber (stream, 2);
		}

		/// <summary>
		///   Parse the milliseconds
		/// </summary>
		private int ParseMilliSeconds (ParseStream stream)
		{
			int ms = 0;

			// Look for fraction
			if (stream.Char == '.')
			{
				stream.Next (true);

				// Parse fraction, can consists of an
				// unlimited sequence of numbers, we only
				// look to the first three (max 1000)
				int count = 0;

				while (stream.Char >= '0' && stream.Char <= '9' &&
						count < 3 && ! stream.EOF)
				{
					ms *= 10;
					ms += stream.Char - '0';

					stream.Next (true);
					count ++;
				}

				if (count == 1) ms *= 100;
				if (count == 2) ms *= 10;
				if (count == 3) ms *= 1;

				// Ignore the rest
				while (stream.Char >= '0' && stream.Char <= '9' &&
						! stream.EOF)
					stream.Next (true);
			}
			return ms;
		}

		/// <summary>
		///   Parse the time zone
		/// </summary>
		private double ParseTimezone (ParseStream stream)
		{
			double timezone = 0;

			SkipWhitespace (stream);

			// Timezone = UTC, use by default 0
			if (stream.Char == 'Z')
				stream.Next (true);
			else
			{
				// Find the sign of the offset
				int sign = 0;

				if (stream.Char == '-')
					sign = -1;

				else if (stream.Char == '+')
					sign = +1;

				else
					throw new Exception ("Invalid time zone: " +
							"unexpected character");

				// Read next char and test for more chars
				stream.Next (true);
				if (stream.EOF)
					throw new Exception ("Invalid time zone");

				// Parse hour offset
				// Note: A hour can be represented by one or two digits.
				string hulp = "";
				while (stream.Char >= '0' &&
						stream.Char <= '9' &&
						!stream.EOF && hulp.Length <= 2)
				{
					hulp += (stream.Char);
					stream.Next (true);
				}
				timezone = sign * Double.Parse (hulp);

				// Parse the minutes of the timezone
				// when there is still more to parse
				if ( ! stream.EOF)
				{
					SkipChar (stream, ':');
					int temp = ParseNumber (stream, 2);

					timezone += (temp / 60.0);
				}
			}

			return timezone;
		}

		/// <summary>
		///   Parse an integer
		/// </summary>
		/// <param name="length">
		///   The number of characters that the integer is expected to be.
		/// </param>
		/// <param name="stream"> The stream that will be parsed </param>
		private int ParseNumber (ParseStream stream, int length)
		{
			System.Text.StringBuilder hulp = new System.Text.StringBuilder ();
		
			int i;
			for (i = 0; i < length && !stream.EOF; i++)
			{
				hulp.Append (stream.Char);
				stream.Next (true);
			}
			if (i == length)
				return Int32.Parse (hulp.ToString ());
			else
				throw new Exception ("Can't parse number");
	
		}

		/// <summary>
		///   Skips a specified char, and throws an exception when
		///   another char was found.
		/// </summary>
		private void SkipChar (ParseStream stream, char toSkip)
		{
			if (stream.Char == toSkip)
				stream.Next (true);
			else
				throw new Exception ("Unexpected character");
		}

		/// <summary>
		///   Skips the spaces * and tabs * in the current stream
		/// </summary>
		private void SkipWhitespace (ParseStream stream)
		{
			while ((stream.Char == ' ' || stream.Char == '\t') && ! stream.EOF)
				stream.Next (true);
		}

		/// <summary> Yaml notation for this datetime </summary>
		private string YamlString ()
		{
			string date = content.ToString ("yyyy-MM-ddTHH:mm:ss");
			int ms = content.Millisecond;
			if (ms != 0)
			{
				string hulp = "" + (ms / 1000.0);
				hulp = hulp.Substring(2); // Cut of the '0,', first 2 digits

				date += "." + hulp;
			}
			string zone = "";

			if (timezone != 0)
			{
				int timezoneHour = (int) Math.Floor (timezone);
				int timezoneMinutes = (int) (60 * (timezone - timezoneHour));
				
				// if positif offset add '+', a '-' is default added
				if (timezone > 0)
					zone = "+";

				zone += timezoneHour.ToString ();
				if (timezoneMinutes != 0)
					zone += ":" + timezoneMinutes;
			}
			else 
				zone = "Z"; // UTC timezone as default and if offset == 0

			return date + zone;
		}

		/// <summary> Write to YAML </summary>
		protected internal override void Write (WriteStream stream)
		{
			stream.Append (YamlString ());
		}
	}
}


