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
using System.Reflection;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public class Utility
	{
		static readonly ConcurrentCache<Type, FieldInfo[]> TypeFields =
			new(type => type.GetFields());

		static readonly ConcurrentCache<(MemberInfo Member, Type AttributeType), bool> MemberHasAttribute =
			new(x => Attribute.IsDefined(x.Member, x.AttributeType));

		static readonly ConcurrentCache<(MemberInfo Member, Type AttributeType, bool Inherit), object[]> MemberCustomAttributes =
			new(x => x.Member.GetCustomAttributes(x.AttributeType, x.Inherit));

		public static FieldInfo[] GetFields(Type type)
		{
			return TypeFields[type];
		}

		public static bool HasAttribute<TAttribute>(MemberInfo member)
			where TAttribute : Attribute
		{
			return MemberHasAttribute[(member, typeof(TAttribute))];
		}

		public static TAttribute[] GetCustomAttributes<TAttribute>(MemberInfo member, bool inherit)
			where TAttribute : Attribute
		{
			return (TAttribute[])MemberCustomAttributes[(member, typeof(TAttribute), inherit)];
		}

		public readonly ModData ModData;
		public readonly InstalledMods Mods;

		public Utility(ModData modData, InstalledMods mods)
		{
			ModData = modData;
			Mods = mods;
		}
	}

	[RequireExplicitImplementation]
	public interface IUtilityCommand
	{
		/// <summary>
		/// The string used to invoke the command.
		/// </summary>
		string Name { get; }

		bool ValidateArguments(string[] args);

		void Run(Utility utility, string[] args);
	}
}
