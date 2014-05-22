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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace OpenRA.Support
{
	public class PerfTimer : IDisposable
	{
		readonly Stopwatch sw;
		readonly string name;
		readonly int thresholdMs;
		readonly int depth;
		readonly PerfTimer parent;
		List<PerfTimer> children;

		static ThreadLocal<PerfTimer> Parent = new ThreadLocal<PerfTimer>();

		// Tree settings
		const int MaxWidth = 60, Digits = 6;
		const int MaxIndentedLabel = MaxWidth - Digits;
		const string IndentationString = "|   ";
		static readonly string FormatString = "{0," + MaxIndentedLabel + "} {1," + Digits + "} ms";

		public PerfTimer(string name, int thresholdMs = 0)
		{
			this.name = name;
			this.thresholdMs = thresholdMs;

			parent = Parent.Value;
			depth = parent == null ? 0 : parent.depth + 1;
			Parent.Value = this;

			sw = Stopwatch.StartNew();
		}

		public void Dispose()
		{
			sw.Stop();

			Parent.Value = parent;

			if (parent == null)
				Write();
			else if (sw.Elapsed.TotalMilliseconds > thresholdMs)
			{
				if (parent.children == null)
					parent.children = new List<PerfTimer>();
				parent.children.Add(this);
			}
		}

		void Write()
		{
			var elapsedMs = Math.Round(this.sw.Elapsed.TotalMilliseconds);

			if (children != null)
			{
				Log.Write("perf", GetHeader(Indentation, this.name));
				foreach (var child in children)
					child.Write();
				Log.Write("perf", FormatString, GetFooter(Indentation), elapsedMs);
			}
			else if (elapsedMs >= thresholdMs)
				Log.Write("perf", FormatString, GetOneLiner(Indentation, this.name), elapsedMs);
		}

		#region Formatting helpers
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

		string Indentation
		{
			get
			{
				if (depth <= 0)
					return string.Empty;
				else if (depth == 1)
					return IndentationString;
				else if (depth == 2)
					return string.Concat(IndentationString, IndentationString);
				else
					return string.Concat(Enumerable.Repeat(IndentationString, depth));
			}
		}
		#endregion
	}
}
