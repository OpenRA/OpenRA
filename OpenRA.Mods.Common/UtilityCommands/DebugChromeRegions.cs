#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class DebugChromeRegions : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--debug-chrome-regions"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 3;
		}

		[Desc("IMAGE", "ZOOM", "Write a html page showing mapped chrome images.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			ChromeProvider.Initialize(modData);

			var image = args[1];
			var zoom = args[2];

			var regions = new List<string>();
			foreach (var c in ChromeProvider.Collections)
			{
				if (c.Value.Image != image)
					continue;

				var pr = c.Value.PanelRegion;
				if (pr != null && pr.Length == 8)
				{
					var sides = new (PanelSides PanelSides, Rectangle Bounds)[]
					{
						(PanelSides.Top | PanelSides.Left, new Rectangle(pr[0], pr[1], pr[2], pr[3])),
						(PanelSides.Top, new Rectangle(pr[0] + pr[2], pr[1], pr[4], pr[3])),
						(PanelSides.Top | PanelSides.Right, new Rectangle(pr[0] + pr[2] + pr[4], pr[1], pr[6], pr[3])),
						(PanelSides.Left, new Rectangle(pr[0], pr[1] + pr[3], pr[2], pr[5])),
						(PanelSides.Center, new Rectangle(pr[0] + pr[2], pr[1] + pr[3], pr[4], pr[5])),
						(PanelSides.Right, new Rectangle(pr[0] + pr[2] + pr[4], pr[1] + pr[3], pr[6], pr[5])),
						(PanelSides.Bottom | PanelSides.Left, new Rectangle(pr[0], pr[1] + pr[3] + pr[5], pr[2], pr[7])),
						(PanelSides.Bottom, new Rectangle(pr[0] + pr[2], pr[1] + pr[3] + pr[5], pr[4], pr[7])),
						(PanelSides.Bottom | PanelSides.Right, new Rectangle(pr[0] + pr[2] + pr[4], pr[1] + pr[3] + pr[5], pr[6], pr[7]))
					};

					foreach (var s in sides)
					{
						var r = s.Bounds;
						if (c.Value.PanelSides.HasSide(s.PanelSides))
							regions.Add("[\"{0}.<{1}>\",{2},{3},{4},{5}]".F(c.Key, s.PanelSides, r.X, r.Y, r.Width, r.Height));
					}
				}

				foreach (var kv in c.Value.Regions)
				{
					var r = kv.Value;
					regions.Add("[\"{0}\",{1},{2},{3},{4}]".F(c.Key + "." + kv.Key, r.X, r.Y, r.Width, r.Height));
				}
			}

			var output = HtmlTemplate.JoinWith("\n").F(zoom, Convert.ToBase64String(modData.ModFiles.Open(image).ReadAllBytes()), "[" + regions.JoinWith(",") + "]");
			var outputPath = Path.ChangeExtension(image, ".html");
			File.WriteAllLines(outputPath, new[] { output });
			Console.WriteLine("Saved {0}", outputPath);
		}

		static readonly string[] HtmlTemplate =
		{
			"<!DOCTYPE html>",
			"<html>",
			"<head>",
			"<meta charset=\"utf-8\"/>",
			"</head>",
			"<body>",
			"<canvas id=\"canvas\" style=\"cursor: crosshair;\"></canvas>",
			"<script>",
			"var zoom = {0};",
			"var chromeImage = \"data:image/png;base64,{1}\";",
			"var chromeRegions = {2}",
			"function setup() {{",
			"	var c = document.getElementById(\"canvas\");",
			"	var ctx = c.getContext(\"2d\");",
			"	var image = new Image;",
			"	image.onload = function() {{",
			"		c.width = zoom*image.width;",
			"		c.height = zoom*image.height;",
			"		ctx.fillStyle = \"#dddddd\";",
			"		for (var j = 0; j < ctx.canvas.height / 4; j++)",
			"			for (var i = j % 2; i < ctx.canvas.width / 4; i += 2)",
			"				ctx.fillRect(4 * i, 4 * j, 4, 4);",
			"		ctx.imageSmoothingEnabled = false;",
			"		ctx.drawImage(image, 0, 0, c.width, c.height);",
			"		ctx.strokeStyle = \"#ffff00\";",
			"		ctx.lineWidth = 1;",
			"		for (var i = 0; i < chromeRegions.length; i++) {{",
			"			var r = chromeRegions[i];",
			"			ctx.strokeRect(zoom*r[1], zoom*r[2], zoom*r[3], zoom*r[4]);",
			"		}}",
			"	}};",
			"	var mouseover = undefined;",
			"	c.addEventListener('mousemove', e => {{",
			"	    var cr = c.getBoundingClientRect();",
			"		var x = (e.clientX - cr.left) / (cr.right - cr.left) * c.width / zoom;",
			"		var y = (e.clientY - cr.top) / (cr.bottom - cr.top) * c.height / zoom;",
			"		var lastover = mouseover;",
			"		mouseover = undefined;",
			"		for (var i = 0; i < chromeRegions.length; i++) {{",
			"			var r = chromeRegions[i];",
			"			if (x >= r[1] && x < r[1] + r[3] && y >= r[2] && y < r[2] + r[4])",
			"				mouseover = r[0];",
			"		}}",
			"		if (lastover != mouseover && mouseover)",
			"			console.log(mouseover);",
			"	}});",
			"	image.src = chromeImage;",
			"}}",
			"window.onload = setup;",
			"</script>",
			"</body>",
			"</html>",
		};
	}
}
