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

using Linguini.Shared.Types.Bundle;

namespace OpenRA
{
	public static class TranslationExts
	{
		public static IFluentType ToFluentType(this object value)
		{
			switch (value)
			{
				case byte number:
					return (FluentNumber)number;
				case sbyte number:
					return (FluentNumber)number;
				case short number:
					return (FluentNumber)number;
				case uint number:
					return (FluentNumber)number;
				case int number:
					return (FluentNumber)number;
				case long number:
					return (FluentNumber)number;
				case ulong number:
					return (FluentNumber)number;
				case float number:
					return (FluentNumber)number;
				case double number:
					return (FluentNumber)number;
				default:
					return (FluentString)value.ToString();
			}
		}
	}
}
