#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Data;
using System.Text;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public static class Extensions
	{
		public static string ToCharacterSeparatedValues(this DataTable table, string delimiter, bool includeHeader)
		{
			var result = new StringBuilder();

			if (includeHeader)
			{
				foreach (DataColumn column in table.Columns)
				{
					result.Append(column.ColumnName);
					result.Append(delimiter);
				}

				result.Remove(result.Length, 0);
				result.AppendLine();
			}

			foreach (DataRow row in table.Rows)
			{
				for (var x = 0; x < table.Columns.Count; x++)
				{
					if (x != 0)
						result.Append(delimiter);

					result.Append(row[table.Columns[x]]);
				}

				result.AppendLine();
			}

			result.Remove(result.Length, 0);
			result.AppendLine();

			return result.ToString();
		}
	}
}