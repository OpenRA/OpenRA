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

using System.Numerics;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public sealed class CncContentLoadScreen : SheetLoadScreen
	{
		Sprite nodLogo, gdiLogo, evaLogo;
		Sprite[] border;
		Vector3 nodPos, gdiPos, evaPos;
		Rectangle bounds;
		string versionText;

		Sheet lastSheet;
		int lastDensity;
		Size lastResolution;

		public override void Init(Manifest manifest, IReadOnlyFileSystem fileSystem)
		{
			base.Init(manifest, fileSystem);

			versionText = manifest.Metadata.Version;
		}

		public override void DisplayInner(Renderer r, Sheet s, int density)
		{
			if (s != lastSheet || density != lastDensity)
			{
				lastSheet = s;
				lastDensity = density;

				border =
				[
					CreateSprite(s, density, new Rectangle(129, 129, 32, 32)),
					CreateSprite(s, density, new Rectangle(161, 129, 62, 32)),
					CreateSprite(s, density, new Rectangle(223, 129, 32, 32)),
					CreateSprite(s, density, new Rectangle(129, 161, 32, 62)),
					null,
					CreateSprite(s, density, new Rectangle(223, 161, 32, 62)),
					CreateSprite(s, density, new Rectangle(129, 223, 32, 32)),
					CreateSprite(s, density, new Rectangle(161, 223, 62, 32)),
					CreateSprite(s, density, new Rectangle(223, 223, 32, 32))
				];

				nodLogo = CreateSprite(s, density, new Rectangle(0, 256, 256, 256));
				gdiLogo = CreateSprite(s, density, new Rectangle(256, 256, 256, 256));
				evaLogo = CreateSprite(s, density, new Rectangle(769, 320, 128, 64));
			}

			if (r.Resolution != lastResolution)
			{
				lastResolution = r.Resolution;

				bounds = new Rectangle(0, 0, lastResolution.Width, lastResolution.Height);
				nodPos = new Vector3(bounds.Width / 2 - 384, bounds.Height / 2 - 128, 0);
				gdiPos = new Vector3(bounds.Width / 2 + 128, bounds.Height / 2 - 128, 0);
				evaPos = new Vector3(bounds.Width - 43 - 128, 43, 0);
			}

			r.RgbaSpriteRenderer.DrawSprite(gdiLogo, gdiPos);
			r.RgbaSpriteRenderer.DrawSprite(nodLogo, nodPos);
			r.RgbaSpriteRenderer.DrawSprite(evaLogo, evaPos);

			WidgetUtils.DrawPanel(bounds, border);

			if (r.Fonts != null)
			{
				var versionFont = r.Fonts["Regular"];
				var versionSize = versionFont.Measure(versionText);
				var versionPos = new Vector2(bounds.Width - 107 - versionSize.X / 2, 115 - versionSize.Y / 2);
				versionFont.DrawTextWithContrast(versionText, versionPos, Color.White, Color.Black, 2);
			}
		}

		public override void StartGame(Arguments args)
		{
			Ui.LoadWidget("MODCONTENT_BACKGROUND", Ui.Root, []);
		}

		public override bool BeforeLoad(ModData modData)
		{
			return true;
		}
	}
}
