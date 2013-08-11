#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Utility;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class D2kExtractGameFilesLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget extractingContainer, copyFilesContainer;

		[ObjectCreator.UseCtor]
		public D2kExtractGameFilesLogic(Widget widget, Action continueLoading)
		{
			panel = widget.Get("EXTRACT_GAMEFILES_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = Extract;

			extractingContainer = panel.Get("EXTRACTING");
			copyFilesContainer = panel.Get("COPY_FILES");

			Extract();
			this.continueLoading = continueLoading;
		}

		void Extract()
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			copyFilesContainer.IsVisible = () => false;
			extractingContainer.IsVisible = () => true;

			var pathToDataR8 = Path.Combine(Platform.SupportDir, "Content/d2k/DATA.R8");
			var pathToPalette = "mods/d2k/bits/d2k.pal";
			var pathToSHPs = Path.Combine(Platform.SupportDir, "Content/d2k/SHPs");
			var pathToTilesets = Path.Combine(Platform.SupportDir, "Content/d2k/Tilesets");

			var extractGameFiles = new string[][]
			{
				new string[] { "--r8", pathToDataR8, pathToPalette, "102", "105", Path.Combine(pathToSHPs, "crates") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "107", "109", Path.Combine(pathToSHPs, "spicebloom") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "114", "129", Path.Combine(pathToSHPs, "rockcrater1") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "130", "145", Path.Combine(pathToSHPs, "rockcrater2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "146", "161", Path.Combine(pathToSHPs, "sandcrater1") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "162", "177", Path.Combine(pathToSHPs, "sandcrater2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1603", "1634", Path.Combine(pathToSHPs, "missiletank"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1635", "1666", Path.Combine(pathToSHPs, "trike"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1667", "1698", Path.Combine(pathToSHPs, "quad"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1699", "1730", Path.Combine(pathToSHPs, "harvester"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1731", "1762", Path.Combine(pathToSHPs, "combata"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1763", "1794", Path.Combine(pathToSHPs, "siegetank"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1795", "1826", Path.Combine(pathToSHPs, "dmcv"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1827", "1858", Path.Combine(pathToSHPs, "sonictank"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1859", "1890", Path.Combine(pathToSHPs, "combataturret"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1891", "1922", Path.Combine(pathToSHPs, "siegeturret"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "1923", "1954", Path.Combine(pathToSHPs, "carryall"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2051", "2082", Path.Combine(pathToSHPs, "combath"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2083", "2114", Path.Combine(pathToSHPs, "devast"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2115", "2146", Path.Combine(pathToSHPs, "combathturret"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2147", "2148", Path.Combine(pathToSHPs, "deathhandmissile") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2389", "2420", Path.Combine(pathToSHPs, "deviatortank"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2421", "2452", Path.Combine(pathToSHPs, "raider"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2453", "2484", Path.Combine(pathToSHPs, "combato"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "2485", "2516", Path.Combine(pathToSHPs, "combatoturret"), "--vehicle" },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4011", "4011", Path.Combine(pathToSHPs, "rifleicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4012", "4012", Path.Combine(pathToSHPs, "bazookaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4013", "4013", Path.Combine(pathToSHPs, "engineericon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4014", "4014", Path.Combine(pathToSHPs, "thumpericon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4015", "4015", Path.Combine(pathToSHPs, "sardaukaricon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4016", "4016", Path.Combine(pathToSHPs, "trikeicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4017", "4017", Path.Combine(pathToSHPs, "raidericon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4018", "4018", Path.Combine(pathToSHPs, "quadicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4019", "4019", Path.Combine(pathToSHPs, "harvestericon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4020", "4020", Path.Combine(pathToSHPs, "combataicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4021", "4021", Path.Combine(pathToSHPs, "combathicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4022", "4022", Path.Combine(pathToSHPs, "combatoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4023", "4023", Path.Combine(pathToSHPs, "mcvicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4024", "4024", Path.Combine(pathToSHPs, "missiletankicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4025", "4025", Path.Combine(pathToSHPs, "deviatortankicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4026", "4026", Path.Combine(pathToSHPs, "siegetankicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4027", "4027", Path.Combine(pathToSHPs, "sonictankicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4028", "4028", Path.Combine(pathToSHPs, "devasticon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4029", "4029", Path.Combine(pathToSHPs, "carryallicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4030", "4030", Path.Combine(pathToSHPs, "carryallicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4031", "4031", Path.Combine(pathToSHPs, "orniicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4032", "4032", Path.Combine(pathToSHPs, "fremenicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4033", "4033", Path.Combine(pathToSHPs, "fremenicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4034", "4034", Path.Combine(pathToSHPs, "saboteuricon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4035", "4035", Path.Combine(pathToSHPs, "deathhandicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4036", "4036", Path.Combine(pathToSHPs, "rifleicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4037", "4037", Path.Combine(pathToSHPs, "bazookaicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4038", "4038", Path.Combine(pathToSHPs, "engineericon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4039", "4039", Path.Combine(pathToSHPs, "thumpericon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4040", "4040", Path.Combine(pathToSHPs, "sardaukaricon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4041", "4041", Path.Combine(pathToSHPs, "trikeicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4042", "4042", Path.Combine(pathToSHPs, "raidericon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4043", "4043", Path.Combine(pathToSHPs, "quadicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4044", "4044", Path.Combine(pathToSHPs, "harvestericon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4045", "4045", Path.Combine(pathToSHPs, "combataicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4046", "4046", Path.Combine(pathToSHPs, "conyardaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4047", "4047", Path.Combine(pathToSHPs, "conyardhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4048", "4048", Path.Combine(pathToSHPs, "conyardoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4049", "4049", Path.Combine(pathToSHPs, "conyardaicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4050", "4050", Path.Combine(pathToSHPs, "4plateaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4051", "4051", Path.Combine(pathToSHPs, "4platehicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4052", "4052", Path.Combine(pathToSHPs, "4plateoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4053", "4053", Path.Combine(pathToSHPs, "6plateaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4054", "4054", Path.Combine(pathToSHPs, "6platehicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4055", "4055", Path.Combine(pathToSHPs, "6plateoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4056", "4056", Path.Combine(pathToSHPs, "pwraicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4057", "4057", Path.Combine(pathToSHPs, "pwrhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4058", "4058", Path.Combine(pathToSHPs, "pwroicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4059", "4059", Path.Combine(pathToSHPs, "barraicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4060", "4060", Path.Combine(pathToSHPs, "barrhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4061", "4061", Path.Combine(pathToSHPs, "barroicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4062", "4062", Path.Combine(pathToSHPs, "orniicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4063", "4063", Path.Combine(pathToSHPs, "wallaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4064", "4064", Path.Combine(pathToSHPs, "wallhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4065", "4065", Path.Combine(pathToSHPs, "walloicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4066", "4066", Path.Combine(pathToSHPs, "refaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4067", "4067", Path.Combine(pathToSHPs, "refhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4068", "4068", Path.Combine(pathToSHPs, "refoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4069", "4069", Path.Combine(pathToSHPs, "guntoweraicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4070", "4070", Path.Combine(pathToSHPs, "guntowerhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4071", "4071", Path.Combine(pathToSHPs, "guntoweroicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4072", "4072", Path.Combine(pathToSHPs, "radaraicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4073", "4073", Path.Combine(pathToSHPs, "radarhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4074", "4074", Path.Combine(pathToSHPs, "radaroicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4075", "4075", Path.Combine(pathToSHPs, "rockettoweraicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4076", "4076", Path.Combine(pathToSHPs, "rockettowerhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4077", "4077", Path.Combine(pathToSHPs, "rockettoweroicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4078", "4078", Path.Combine(pathToSHPs, "hightechaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4079", "4079", Path.Combine(pathToSHPs, "hightechhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4080", "4080", Path.Combine(pathToSHPs, "hightechoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4081", "4081", Path.Combine(pathToSHPs, "lightaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4082", "4082", Path.Combine(pathToSHPs, "lighthicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4083", "4083", Path.Combine(pathToSHPs, "lightoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4084", "4084", Path.Combine(pathToSHPs, "siloaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4085", "4085", Path.Combine(pathToSHPs, "silohicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4086", "4086", Path.Combine(pathToSHPs, "silooicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4087", "4087", Path.Combine(pathToSHPs, "heavyaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4088", "4088", Path.Combine(pathToSHPs, "heavyhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4089", "4089", Path.Combine(pathToSHPs, "heavyoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4090", "4090", Path.Combine(pathToSHPs, "orniicon3") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4091", "4091", Path.Combine(pathToSHPs, "heavyhicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4092", "4092", Path.Combine(pathToSHPs, "starportaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4093", "4093", Path.Combine(pathToSHPs, "starporthicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4094", "4094", Path.Combine(pathToSHPs, "starportoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4095", "4095", Path.Combine(pathToSHPs, "orniicon4") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4096", "4096", Path.Combine(pathToSHPs, "repairaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4097", "4097", Path.Combine(pathToSHPs, "repairhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4098", "4098", Path.Combine(pathToSHPs, "repairoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4099", "4099", Path.Combine(pathToSHPs, "researchaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4100", "4100", Path.Combine(pathToSHPs, "researchhicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4101", "4101", Path.Combine(pathToSHPs, "researchoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4102", "4102", Path.Combine(pathToSHPs, "palaceaicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4103", "4103", Path.Combine(pathToSHPs, "palacehicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4104", "4104", Path.Combine(pathToSHPs, "palaceoicon") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4105", "4105", Path.Combine(pathToSHPs, "orniicon5") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4106", "4106", Path.Combine(pathToSHPs, "radaraicon2") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4107", "4107", Path.Combine(pathToSHPs, "radaraicon3") },
				new string[] { "--r8", pathToDataR8, pathToPalette, "4108", "4108", Path.Combine(pathToSHPs, "conyardaicon3") },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "BASE"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), pathToPalette, "748", "749", Path.Combine(pathToSHPs, "spice0") },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBAT.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "BAT"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBGBS.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "BGBS"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXICE.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "ICE"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXTREE.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "TREE"), "--tileset" },
				new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXWAST.R8"), pathToPalette, "0", "799", Path.Combine(pathToTilesets, "WAST"), "--tileset" },
				////new string[] { "--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXXMAS.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "XMAS"), "--tileset" },
			};

			var shpToCreate = new string[][]
			{
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockcrater1.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockcrater2.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sandcrater1.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sandcrater2.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "missiletank.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "trike.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "quad.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "harvester.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combata.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "siegetank.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "dmcv.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sonictank.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combataturret.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "siegeturret.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "carryall.png"), "64" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combath.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "devast.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combathturret.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "deviatortank.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "raider.png"), "32" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combato.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combatoturret.png"), "48" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "rifleicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "bazookaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "engineericon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "thumpericon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sardaukaricon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "trikeicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "raidericon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "quadicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "harvestericon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combataicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combathicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "combatoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "mcvicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "missiletankicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "deviatortankicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "siegetankicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "sonictankicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "devasticon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "carryallicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "orniicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "fremenicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "saboteuricon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "deathhandicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "conyardaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "conyardhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "conyardoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "4plateaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "4platehicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "4plateoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "6plateaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "6platehicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "6plateoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "pwraicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "pwrhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "pwroicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "barraicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "barrhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "barroicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "wallaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "wallhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "walloicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "refaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "refhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "refoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "guntoweraicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "guntowerhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "guntoweroicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "radaraicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "radarhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "radaroicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockettoweraicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockettowerhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "rockettoweroicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "hightechaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "hightechhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "hightechoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "lightaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "lighthicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "lightoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "siloaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "silohicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "silooicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "heavyaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "heavyhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "heavyoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "starportaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "starporthicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "starportoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "repairaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "repairhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "repairoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "researchaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "researchhicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "researchoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "palaceaicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "palacehicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "palaceoicon.png"), "60" },
				new string[] { "--shp", Path.Combine(pathToSHPs, "spice0.png"), "32" },
			};

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: " + s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			var t = new Thread(_ =>
			{
				try
				{
        			for (int i = 0; i < extractGameFiles.Length; i++)
					{
						progressBar.Percentage = i * 100 / extractGameFiles.Count();
						statusLabel.GetText = () => "Extracting...";
						Utility.Command.ConvertR8ToPng(extractGameFiles[i]);
					}

					for (int i = 0; i < shpToCreate.Length; i++)
					{
						progressBar.Percentage = i * 100 / shpToCreate.Count();
						statusLabel.GetText = () => "Converting...";
						Utility.Command.ConvertPngToShp(shpToCreate[i]);
						File.Delete(shpToCreate[i][1]);
					}

					statusLabel.GetText = () => "Building tilesets...";
					int c = 0;
					string[] TilesetArray = new string[] { "BASE", "BAT", "BGBS", "ICE", "TREE", "WAST" };
					foreach (string set in TilesetArray)
					{
						progressBar.Percentage = c * 100 / TilesetArray.Count();
						File.Delete(Path.Combine(pathToTilesets, "{0}.tsx".F(set)));
						File.Copy("mods/d2k/tilesets/{0}.tsx".F(set), Path.Combine(pathToTilesets, "{0}.tsx".F(set)));

						// TODO: this is ugly: a GUI will open and close immediately after some delay
						Process p = new Process();
						ProcessStartInfo TilesetBuilderProcessStartInfo = new ProcessStartInfo("OpenRA.TilesetBuilder.exe", Path.Combine(pathToTilesets, "{0}.png".F(set)) + " 32 --export Content/d2k/Tilesets");
						p.StartInfo = TilesetBuilderProcessStartInfo;
						p.Start();
						p.WaitForExit();
						File.Delete(Path.Combine(pathToTilesets, "{0}.tsx".F(set)));
						File.Delete(Path.Combine(pathToTilesets, "{0}.png".F(set)));
						File.Delete(Path.Combine(pathToTilesets, "{0}.yaml".F(set.ToLower())));
						File.Delete(Path.Combine(pathToTilesets, "{0}.pal".F(set.ToLower())));
						c++;
					}

					Game.RunAfterTick(() =>
					{
						progressBar.Percentage = 100;
						statusLabel.GetText = () => "Extraction and conversion complete.";
						backButton.IsDisabled = () => false;
						continueLoading();
					});
				}
				catch
				{
					onError("Installation failed");
				}
			}) { IsBackground = true };
			t.Start();
		}
	}
}
