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

using System;
using System.IO;
using StyleCop;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CheckCodeStyle : IUtilityCommand
	{
		public string Name { get { return "--check-code-style"; } }
		int violationCount;

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("DIRECTORY", "Check the *.cs source code files in a directory for code style violations.")]
		public void Run(ModData modData, string[] args)
		{
			var relativePath = args[1];
			var projectPath = Path.GetFullPath(relativePath);

			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var console = new StyleCopConsole(null, false, null, null, true);
			var project = new CodeProject(0, projectPath, new Configuration(null));
			foreach (var filePath in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
				console.Core.Environment.AddSourceCode(project, filePath, null);

			console.ViolationEncountered += OnViolationEncountered;
			console.Start(new[] { project }, true);

			if (violationCount > 0)
				Environment.Exit(1);
			else
				Console.WriteLine("No violations encountered in {0}.".F(relativePath));
		}

		void OnViolationEncountered(object sender, ViolationEventArgs e)
		{
			violationCount++;
			var path = e.SourceCode.Path.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "");
			Console.WriteLine("{0}:L{1}: [{2}] {3}", path, e.LineNumber, e.Violation.Rule.CheckId, e.Message);
		}
	}
}
