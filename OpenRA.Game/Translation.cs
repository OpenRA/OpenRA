#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluent.Net;
using Fluent.Net.RuntimeAst;
using OpenRA.FileSystem;

namespace OpenRA
{
	public class Translation
	{
		readonly IEnumerable<MessageContext> messageContexts;

		public Translation(string language, string[] translations, IReadOnlyFileSystem fileSystem)
		{
			if (translations == null || !translations.Any())
				return;

			messageContexts = GetMessageContext(language, translations, fileSystem).ToList();
		}

		IEnumerable<MessageContext> GetMessageContext(string language, string[] translations, IReadOnlyFileSystem fileSystem)
		{
			var backfall = translations.Where(t => t.EndsWith("en.ftl"));
			var paths = translations.Where(t => t.EndsWith(language + ".ftl"));
			foreach (var path in paths.Concat(backfall))
			{
				var stream = fileSystem.Open(path);
				using (var reader = new StreamReader(stream))
				{
					var options = new MessageContextOptions { UseIsolating = false };
					var messageContext = new MessageContext(language, options);
					var errors = messageContext.AddMessages(reader);
					foreach (var error in errors)
						Log.Write("debug", error.ToString());

					yield return messageContext;
				}
			}
		}

		public string GetFormattedMessage(string key, IDictionary<string, object> args = null, string attribute = null)
		{
			if (key == null)
				return "";

			foreach (var messageContext in messageContexts)
			{
				var message = messageContext.GetMessage(key);
				if (message != null)
				{
					if (string.IsNullOrEmpty(attribute))
						return messageContext.Format(message, args);
					else
						return messageContext.Format(message.Attributes[attribute], args);
				}
			}

			return key;
		}

		public string GetAttribute(string key, string attribute)
		{
			if (key == null)
				return "";

			foreach (var messageContext in messageContexts)
			{
				var message = messageContext.GetMessage(key);
				if (message != null && message.Attributes != null && message.Attributes.ContainsKey(attribute))
				{
					var node = message.Attributes[attribute];
					var stringLiteral = (StringLiteral)node;
					return stringLiteral.Value;
				}
			}

			return "";
		}

		// Adapted from Fluent.Net.SimpleExample.TranslationService by Mark Weaver
		public static Dictionary<string, object> Arguments(string name, object value, params object[] args)
		{
			if (args.Length % 2 != 0)
				throw new ArgumentException("Expected a comma separated list of name, value arguments"
					+ " but the number of arguments is not a multiple of two", nameof(args));

			var argumentDictionary = new Dictionary<string, object> { { name, value } };

			for (var i = 0; i < args.Length; i += 2)
			{
				name = args[i] as string;
				if (string.IsNullOrEmpty(name))
				{
					throw new ArgumentException($"Expected the argument at index {i} to be a non-empty string",
						nameof(args));
				}

				value = args[i + 1];
				if (value == null)
					throw new ArgumentNullException("args", $"Expected the argument at index {i + 1} to be a non-null value");

				argumentDictionary.Add(name, value);
			}

			return argumentDictionary;
		}
	}
}
