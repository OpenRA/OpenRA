#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.Common.Terrain
{
	/// <summary>
	/// Information about how certain templates (like cliffs, beaches, roads) link together.
	/// </summary>
	public class TemplateSegment
	{
		public readonly string Start;
		public readonly string Inner;
		public readonly string End;

		/// <summary>
		/// Point sequence, where points are -X-Y corners of template tiles.
		/// </summary>
		[FieldLoader.Ignore]
		public readonly CVec[] Points;

		public TemplateSegment(MiniYaml my)
		{
			FieldLoader.Load(this, my);
			{
				// Unlike FieldLoader.ParseInt2Array, whitespace is ignored.
				var value = my.NodeWithKey("Points").Value.Value;
				var parts = Regex.Replace(value, @"\s+", string.Empty)
					.Split(',', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length % 2 != 0)
					FieldLoader.InvalidValueAction(value, typeof(int2[]), "Points");
				Points = new CVec[parts.Length / 2];
				for (var i = 0; i < Points.Length; i++)
					Points[i] = new CVec(Exts.ParseInt32Invariant(parts[2 * i]), Exts.ParseInt32Invariant(parts[2 * i + 1]));
			}
		}

		public static bool MatchesType(string type, string matcher)
		{
			if (type == matcher)
				return true;

			return type.StartsWith($"{matcher}.", StringComparison.InvariantCulture);
		}

		public bool HasStartType(string matcher)
			=> MatchesType(Start, matcher);
		public bool HasInnerType(string matcher)
			=> MatchesType(Inner, matcher);
		public bool HasEndType(string matcher)
			=> MatchesType(End, matcher);
	}
}
