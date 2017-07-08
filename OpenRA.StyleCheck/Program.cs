#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, and is made available under the terms of the MS-PL license.
 * For more information, see https://opensource.org/licenses/MS-PL
 */
#endregion

using System;
using System.IO;
using StyleCop;

namespace OpenRA.StyleCheck
{
	class StyleCheck
	{
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("Usage: OpenRA.StyleCheck.exe DIRECTORY");
				Console.WriteLine("Check the *.cs source code files in a directory for code style violations.");
				return;
			}

			var projectPath = Path.GetFullPath(args[0]);
			var console = new StyleCopConsole(null, false, null, null, true);
			var project = new CodeProject(0, projectPath, new Configuration(null));
			foreach (var filePath in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
				console.Core.Environment.AddSourceCode(project, filePath, null);

			var violationCount = 0;
			console.ViolationEncountered += (object sender, ViolationEventArgs e) => {
				violationCount++;
				var path = e.SourceCode.Path.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "");
				Console.WriteLine("{0}:L{1}: [{2}] {3}", path, e.LineNumber, e.Violation.Rule.CheckId, e.Message);
			};

			console.Start(new[] { project }, true);

			if (violationCount > 0)
				Environment.Exit(1);
			else
				Console.WriteLine("No violations encountered in {0}.", args[0]);
		}
	}
}
