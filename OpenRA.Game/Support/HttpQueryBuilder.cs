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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OpenRA.Support
{
	public class HttpQueryBuilder : IEnumerable<KeyValuePair<string, string>>
	{
		readonly string url;
		readonly List<KeyValuePair<string, string>> parameters = new();

		public HttpQueryBuilder(string url)
		{
			this.url = url;
		}

		public void Add(string name, object value)
		{
			parameters.Add(KeyValuePair.Create(
				name,
				Uri.EscapeDataString(value.ToString())));
		}

		public override string ToString()
		{
			var builder = new StringBuilder(url);

			builder.Append('?');

			foreach (var parameter in parameters)
				builder.Append($"{parameter.Key}={parameter.Value}&");

			return builder.ToString();
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return parameters.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
