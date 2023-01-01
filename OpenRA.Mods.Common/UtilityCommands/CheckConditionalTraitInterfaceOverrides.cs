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
using System.Linq;
using System.Reflection;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class CheckConditionalTraitInterfaceOverrides : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-conditional-trait-interface-overrides";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 1;
		}

		int violationCount;

		static bool IsConditionalTrait(Type type)
		{
			// Walk up the inheritance chain to check if any parent type is the generic ConditionalTrait type
			while (type != null && type != typeof(object))
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ConditionalTrait<>))
					return true;

				type = type.BaseType;
			}

			return false;
		}

		void CheckInterfaceViolation(Utility utility, Type interfaceType, string methodName)
		{
			var types = utility.ModData.ObjectCreator.GetTypes()
				.Where(t => interfaceType.IsAssignableFrom(t) && !t.IsGenericType);

			foreach (var t in types)
			{
				if (!IsConditionalTrait(t))
					continue;

				var overridesCreated = t.GetMethod($"{interfaceType.FullName}.{methodName}", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null;
				if (overridesCreated)
				{
					Console.WriteLine("{0} must override ConditionalTrait's {1} method instead of implementing {2} directly", t.Name, methodName, interfaceType.Name);
					violationCount++;
				}
			}
		}

		[Desc("Check for incorrect interface overrides in conditional traits defined in all assemblies referenced by the specified mod.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			CheckInterfaceViolation(utility, typeof(INotifyCreated), "Created");
			CheckInterfaceViolation(utility, typeof(IObservesVariables), "GetVariableObservers");

			if (violationCount > 0)
			{
				Console.WriteLine("Interface override violations: {0}", violationCount);
				Environment.Exit(1);
			}
		}
	}
}
