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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class CheckExplicitInterfacesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-explicit-interfaces";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 1;
		}

		int violationCount;

		[Desc("Check for explicit interface implementation violations in all assemblies referenced by the specified mod.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var types = utility.ModData.ObjectCreator.GetTypes();

			foreach (var implementingType in types.Where(t => !t.IsInterface))
			{
				if (implementingType.IsEnum)
					continue;

				var interfaces = implementingType.GetInterfaces();
				foreach (var interfaceType in interfaces)
				{
					if (!interfaceType.HasAttribute<RequireExplicitImplementationAttribute>())
						continue;

					var interfaceMembers = interfaceType.GetMembers();
					foreach (var interfaceMember in interfaceMembers)
					{
						if (interfaceMember.Name.StartsWith("get_") || interfaceMember.Name.StartsWith("set_") || interfaceMember.Name.StartsWith("add_") || interfaceMember.Name.StartsWith("remove_"))
							continue;

						var interfaceMethod = interfaceMember as MethodInfo;
						if (interfaceMethod != null)
						{
							var interfaceMethodParams = interfaceMethod.GetParameters();
							foreach (var implementingMethod in implementingType.GetMethods())
							{
								if (implementingMethod.Name != interfaceMethod.Name
									|| implementingMethod.ReturnType != interfaceMethod.ReturnType)
									continue;

								var implementingMethodParams = implementingMethod.GetParameters();
								var lenImpl = implementingMethodParams.Length;
								if (lenImpl != interfaceMethodParams.Length)
									continue;

								var allMatch = true;
								for (var i = 0; i < lenImpl; i++)
								{
									var implementingParam = implementingMethodParams[i];
									var interfaceParam = interfaceMethodParams[i];
									if (implementingParam.ParameterType != interfaceParam.ParameterType
										|| implementingParam.Name != interfaceParam.Name
										|| implementingParam.IsOut != interfaceParam.IsOut)
									{
										allMatch = false;
										break;
									}
								}

								// Explicitly implemented methods are never public in C#.
								if (allMatch && implementingMethod.IsPublic)
									OnViolation(implementingType, interfaceType, implementingMethod);
							}
						}

						var interfaceProperty = interfaceMember as PropertyInfo;
						if (interfaceProperty != null)
						{
							var implementingProperties = implementingType.GetProperties();
							foreach (var implementingProperty in implementingProperties)
							{
								if (implementingProperty.PropertyType != interfaceProperty.PropertyType
									|| implementingProperty.Name != interfaceProperty.Name)
									continue;

								if (!IsExplicitInterfaceProperty(implementingProperty))
									OnViolation(implementingType, interfaceType, implementingProperty);
							}
						}
					}
				}
			}

			if (violationCount > 0)
			{
				Console.WriteLine($"Explicit interface violations: {violationCount}");
				Environment.Exit(1);
			}
		}

		static bool IsExplicitInterfaceProperty(PropertyInfo pi)
		{
			return pi.Name.Contains('.');
		}

		void OnViolation(Type implementor, Type interfaceType, MemberInfo violator)
		{
			Console.WriteLine($"{implementor.Name} must explicitly implement the interface member {interfaceType.Name}.{violator.Name}");
			violationCount++;
		}
	}
}
