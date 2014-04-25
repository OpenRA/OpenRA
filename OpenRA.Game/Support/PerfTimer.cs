#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Threading;

namespace OpenRA.Support
{
	public class PerfTimer : IDisposable
	{
		readonly Stopwatch sw = new Stopwatch();
		readonly string Name;

		//
		// Hacks to give the output a tree-like structure
		//
		static ThreadLocal<int> depth = new ThreadLocal<int>();
		static ThreadLocal<string> prevHeader = new ThreadLocal<string>();
		const int MaxWidth = 60, Digits = 6;
		const int MaxIndentedLabel = MaxWidth - Digits;
		const string IndentationString = "|   ";
		readonly string FormatString = "{0," + MaxIndentedLabel + "} {1," + Digits + "} ms";

		public PerfTimer(string name)
		{
			if (prevHeader.Value != null)
			{
				Log.Write("perf", prevHeader.Value);
				prevHeader.Value = null;
			}

			this.Name = name;

			prevHeader.Value = GetHeader(Indentation, this.Name);
			depth.Value++;
		}

		public void Dispose()
		{
			depth.Value--;

			string s;

			if (prevHeader.Value == null)
			{
				s = GetFooter(Indentation);
			}
			else
			{
				s = GetOneLiner(Indentation, this.Name);
				prevHeader.Value = null;
			}

			Log.Write("perf", FormatString, s, Math.Round(this.sw.Elapsed.TotalMilliseconds));
		}

		static string GetHeader(string indentation, string label)
		{
			return string.Concat(indentation, LimitLength(label, MaxIndentedLabel - indentation.Length));
		}

		static string GetOneLiner(string indentation, string label)
		{
			return string.Concat(indentation, SetLength(label, MaxIndentedLabel - indentation.Length));
		}

		static string GetFooter(string indentation)
		{
			return string.Concat(indentation, new string('-', MaxIndentedLabel - indentation.Length));
		}

		static string LimitLength(string s, int length, int minLength = 8)
		{
			length = Math.Max(length, minLength);

			if (s == null || s.Length <= length)
				return s;

			return s.Substring(0, length);
		}

		static string SetLength(string s, int length, int minLength = 8)
		{
			length = Math.Max(length, minLength);

			if (s == null || s.Length == length)
				return s;

			if (s.Length < length)
				return s.PadRight(length);

			return s.Substring(0, length);
		}

		static string Indentation
		{
			get
			{
				var d = depth.Value;
				if (d == 1)
					return IndentationString;
				else if (d <= 0)
					return string.Empty;
				else
					return string.Concat(Enumerable.Repeat(IndentationString, depth.Value));
			}
		}
	}
}
