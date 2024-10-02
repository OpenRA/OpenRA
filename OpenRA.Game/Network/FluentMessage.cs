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
using System.Collections.Generic;
using Linguini.Shared.Types.Bundle;

namespace OpenRA.Network
{
	public class FluentArgument
	{
		public enum FluentArgumentType
		{
			String = 0,
			Number = 1,
		}

		public readonly string Key;
		public readonly string Value;
		public readonly FluentArgumentType Type;

		public FluentArgument() { }

		public FluentArgument(string key, object value)
		{
			Key = key;
			Value = value.ToString();
			Type = GetFluentArgumentType(value);
		}

		static FluentArgumentType GetFluentArgumentType(object value)
		{
			switch (value.ToFluentType())
			{
				case FluentNumber:
					return FluentArgumentType.Number;
				default:
					return FluentArgumentType.String;
			}
		}
	}

	public class FluentMessage
	{
		public const int ProtocolVersion = 1;

		public readonly string Key = string.Empty;

		[FieldLoader.LoadUsing(nameof(LoadArguments))]
		public readonly object[] Arguments;

		static object LoadArguments(MiniYaml yaml)
		{
			var arguments = new List<object>();
			var argumentsNode = yaml.NodeWithKeyOrDefault("Arguments");
			if (argumentsNode != null)
			{
				foreach (var argumentNode in argumentsNode.Value.Nodes)
				{
					var argument = FieldLoader.Load<FluentArgument>(argumentNode.Value);
					arguments.Add(argument.Key);
					if (argument.Type == FluentArgument.FluentArgumentType.Number)
					{
						if (!double.TryParse(argument.Value, out var number))
							Log.Write("debug", $"Failed to parse {argument.Value}");

						arguments.Add(number);
					}
					else
						arguments.Add(argument.Value);
				}
			}

			return arguments.ToArray();
		}

		public FluentMessage(MiniYaml yaml)
		{
			// Let the FieldLoader do the dirty work of loading the public fields.
			FieldLoader.Load(this, yaml);
		}

		public static string Serialize(string key, object[] args)
		{
			var root = new List<MiniYamlNode>
			{
				new("Protocol", ProtocolVersion.ToStringInvariant()),
				new("Key", key),
			};

			if (args != null)
			{
				var nodes = new List<MiniYamlNode>();
				for (var i = 0; i < args.Length; i += 2)
				{
					var argKey = args[i] as string;
					if (string.IsNullOrEmpty(argKey))
						throw new ArgumentException($"Expected the argument at index {i} to be a non-empty string", nameof(args));

					var argValue = args[i + 1];
					if (argValue == null)
						throw new ArgumentNullException(nameof(args), $"Expected the argument at index {i + 1} to be a non-null value");

					nodes.Add(new MiniYamlNode($"Argument@{i / 2}", FieldSaver.Save(new FluentArgument(argKey, argValue))));
				}

				root.Add(new MiniYamlNode("Arguments", new MiniYaml("", nodes)));
			}

			return new MiniYaml("", root)
				.ToLines("FluentMessage")
				.JoinWith("\n");
		}
	}
}
