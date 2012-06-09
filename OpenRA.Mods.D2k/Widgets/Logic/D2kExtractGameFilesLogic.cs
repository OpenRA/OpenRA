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
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Widgets;
using OpenRA.Utility;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class D2kExtractGameFilesLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget extractingContainer, copyFilesContainer;

		[ObjectCreator.UseCtor]
		public D2kExtractGameFilesLogic(Widget widget)
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
		}

		void Extract()
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			copyFilesContainer.IsVisible = () => false;
			extractingContainer.IsVisible = () => true;

			var PathToDataR8 = Path.Combine(Platform.SupportDir, "Content/d2k/DATA.R8");
			var PathToPalette = "mods/d2k/bits/d2k.pal";
			var PathToImages = Path.Combine(Platform.SupportDir, "Content/d2k/SHPs");

			var ExtractGameFiles = new string[][]
			{
				new string[] {"--r8", PathToDataR8, PathToPalette, "0", "2", Path.Combine(PathToImages, "overlay")},
				//new string[] {"--r8", PathToDataR8, PathToPalette, "40", "101", Path.Combine(PathToImages, "shadow")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "102", "105", Path.Combine(PathToImages, "crates")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "107", "109", Path.Combine(PathToImages, "spicebloom")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "114", "129", Path.Combine(PathToImages, "rockcrater1")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "130", "145", Path.Combine(PathToImages, "rockcrater2")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "146", "161", Path.Combine(PathToImages, "sandcrater1")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "162", "177", Path.Combine(PathToImages, "sandcrater2")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "206", "381", Path.Combine(PathToImages, "rifle"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "382", "457", Path.Combine(PathToImages, "rifledeath"), "--infantrydeath"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "458", "693", Path.Combine(PathToImages, "rocket"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "694", "929", Path.Combine(PathToImages, "fremen"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "930", "1165", Path.Combine(PathToImages, "sardaukar"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1166", "1221", Path.Combine(PathToImages, "engineer"), "--infantry"}, // death animation 1342..1401
				new string[] {"--r8", PathToDataR8, PathToPalette, "1402", "1502", Path.Combine(PathToImages, "thumper"), "--infantry"}, // death animations 1543..1602
				new string[] {"--r8", PathToDataR8, PathToPalette, "1603", "1634", Path.Combine(PathToImages, "missile"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1635", "1666", Path.Combine(PathToImages, "trike"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1667", "1698", Path.Combine(PathToImages, "quad"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1699", "1730", Path.Combine(PathToImages, "harvester"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1731", "1762", Path.Combine(PathToImages, "combata"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1763", "1794", Path.Combine(PathToImages, "siege"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1795", "1826", Path.Combine(PathToImages, "dmcv"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1827", "1858", Path.Combine(PathToImages, "sonic"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1859", "1890", Path.Combine(PathToImages, "combataturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1891", "1922", Path.Combine(PathToImages, "siegeturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1923", "1954", Path.Combine(PathToImages, "carryall"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1955", "2050", Path.Combine(PathToImages, "orni"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2051", "2082", Path.Combine(PathToImages, "combath"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2083", "2114", Path.Combine(PathToImages, "devast"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2115", "2146", Path.Combine(PathToImages, "combathturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2147", "2148", Path.Combine(PathToImages, "deathhandmissile")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2245", "2284", Path.Combine(PathToImages, "saboteur"), "--infantry"}, //#death animations 2325..2388
				//rifleinfantry repetitions?
				new string[] {"--r8", PathToDataR8, PathToPalette, "2389", "2420", Path.Combine(PathToImages, "deviator"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2421", "2452", Path.Combine(PathToImages, "raider"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2453", "2484", Path.Combine(PathToImages, "combato"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2485", "2516", Path.Combine(PathToImages, "combatoturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2517", "2517", Path.Combine(PathToImages, "frigate"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2518", "2520", Path.Combine(PathToImages, "heavya"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2521", "2522", Path.Combine(PathToImages, "radara"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2523", "2524", Path.Combine(PathToImages, "pwra"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2525", "2526", Path.Combine(PathToImages, "barra"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2527", "2558", Path.Combine(PathToImages, "wall"), "--wall"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2559", "2560", Path.Combine(PathToImages, "conyarda"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2561", "2563", Path.Combine(PathToImages, "refa"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2564", "2565", Path.Combine(PathToImages, "hightecha"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2566", "2570", Path.Combine(PathToImages, "siloa"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2571", "2572", Path.Combine(PathToImages, "repaira"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2573", "2588", Path.Combine(PathToImages, "guntower"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2589", "2620", Path.Combine(PathToImages, "gunturret"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2621", "2636", Path.Combine(PathToImages, "rockettower"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2637", "2668", Path.Combine(PathToImages, "rocketturreta"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2669", "2670", Path.Combine(PathToImages, "researcha"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2671", "2672", Path.Combine(PathToImages, "starporta"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2673", "2675", Path.Combine(PathToImages, "lighta"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2676", "2677", Path.Combine(PathToImages, "palacea"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2678", "2680", Path.Combine(PathToImages, "heavyh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2681", "2682", Path.Combine(PathToImages, "radarh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2683", "2684", Path.Combine(PathToImages, "pwrh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2685", "2686", Path.Combine(PathToImages, "barrh"), "--building"},
				// identical wall
				new string[] {"--r8", PathToDataR8, PathToPalette, "2719", "2720", Path.Combine(PathToImages, "conyardh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2721", "2723", Path.Combine(PathToImages, "refh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2724", "2725", Path.Combine(PathToImages, "hightechh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2726", "2730", Path.Combine(PathToImages, "siloh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2731", "2732", Path.Combine(PathToImages, "repairh"), "--building"},
				// identical guntower
				new string[] {"--r8", PathToDataR8, PathToPalette, "2749", "2780", Path.Combine(PathToImages, "gunturreth"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2797", "2828", Path.Combine(PathToImages, "rocketturreth"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2829", "2830", Path.Combine(PathToImages, "researchh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2831", "2832", Path.Combine(PathToImages, "starporth"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2833", "2835", Path.Combine(PathToImages, "lighth"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2836", "2837", Path.Combine(PathToImages, "palaceh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2838", "2840", Path.Combine(PathToImages, "heavyo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2841", "2842", Path.Combine(PathToImages, "radaro"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2843", "2844", Path.Combine(PathToImages, "pwro"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2845", "2846", Path.Combine(PathToImages, "barro"), "--building"},
				// identical wall
				new string[] {"--r8", PathToDataR8, PathToPalette, "2879", "2880", Path.Combine(PathToImages, "conyardo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2881", "2883", Path.Combine(PathToImages, "refo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2884", "2885", Path.Combine(PathToImages, "hightecho"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2886", "2890", Path.Combine(PathToImages, "siloo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2891", "2892", Path.Combine(PathToImages, "repairo"), "--building"},
				// identical guntower
				new string[] {"--r8", PathToDataR8, PathToPalette, "2909", "2940", Path.Combine(PathToImages, "gunturreto"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2957", "2988", Path.Combine(PathToImages, "rocketturreto"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2989", "2990", Path.Combine(PathToImages, "researcho"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2991", "2992", Path.Combine(PathToImages, "starporto"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2993", "2995", Path.Combine(PathToImages, "lighto"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2996", "2997", Path.Combine(PathToImages, "palaceo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3549", "3564", Path.Combine(PathToImages, "sandwormmouth")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3565", "3585", Path.Combine(PathToImages, "sandwormdust")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3586", "3600", Path.Combine(PathToImages, "wormsigns1")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3601", "3610", Path.Combine(PathToImages, "wormsigns2")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3611", "3615", Path.Combine(PathToImages, "wormsigns3")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3616", "3620", Path.Combine(PathToImages, "wormsigns4")},
				//new string[] {"--r8", PathToDataR8, PathToPalette, "3679", "3686", "sell"},
				//explosions and muzzle flash
				new string[] {"--r8", PathToDataR8, PathToPalette, "4011", "4011", Path.Combine(PathToImages, "rifleicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4012", "4012", Path.Combine(PathToImages, "bazookaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4013", "4013", Path.Combine(PathToImages, "engineericon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4014", "4014", Path.Combine(PathToImages, "thumpericon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4015", "4015", Path.Combine(PathToImages, "sadaukaricon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4016", "4016", Path.Combine(PathToImages, "trikeicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4017", "4017", Path.Combine(PathToImages, "raidericon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4018", "4018", Path.Combine(PathToImages, "quadicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4019", "4019", Path.Combine(PathToImages, "harvestericon")}, // == 4044
				new string[] {"--r8", PathToDataR8, PathToPalette, "4020", "4020", Path.Combine(PathToImages, "combataicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4021", "4021", Path.Combine(PathToImages, "combathicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4022", "4022", Path.Combine(PathToImages, "combatoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4023", "4023", Path.Combine(PathToImages, "mcvicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4024", "4024", Path.Combine(PathToImages, "missileicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4025", "4025", Path.Combine(PathToImages, "deviatoricon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4026", "4026", Path.Combine(PathToImages, "siegeicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4027", "4027", Path.Combine(PathToImages, "sonicicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4028", "4028", Path.Combine(PathToImages, "devasticon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4029", "4029", Path.Combine(PathToImages, "carryallicon")}, // == 4030
				new string[] {"--r8", PathToDataR8, PathToPalette, "4031", "4031", Path.Combine(PathToImages, "orniicon")}, // == 4062
				new string[] {"--r8", PathToDataR8, PathToPalette, "4032", "4032", Path.Combine(PathToImages, "fremenicon")}, // == 4033
				new string[] {"--r8", PathToDataR8, PathToPalette, "4034", "4034", Path.Combine(PathToImages, "saboteuricon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4035", "4035", Path.Combine(PathToImages, "deathhandicon")},
				// "4036..4045 = repetitions
				new string[] {"--r8", PathToDataR8, PathToPalette, "4046", "4046", Path.Combine(PathToImages, "conyardaicon")}, // == 4049
				new string[] {"--r8", PathToDataR8, PathToPalette, "4047", "4047", Path.Combine(PathToImages, "conyardhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4048", "4048", Path.Combine(PathToImages, "conyardoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4050", "4050", Path.Combine(PathToImages, "4plateicon")}, // == 4051..4052
				new string[] {"--r8", PathToDataR8, PathToPalette, "4053", "4053", Path.Combine(PathToImages, "6plateicon")}, // == 4054..4055
				new string[] {"--r8", PathToDataR8, PathToPalette, "4056", "4056", Path.Combine(PathToImages, "pwraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4057", "4057", Path.Combine(PathToImages, "pwrhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4058", "4058", Path.Combine(PathToImages, "pwroicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4059", "4059", Path.Combine(PathToImages, "barraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4060", "4060", Path.Combine(PathToImages, "barrhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4061", "4061", Path.Combine(PathToImages, "barroicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4063", "4063", Path.Combine(PathToImages, "wallicon")}, // == 4061..4062
				new string[] {"--r8", PathToDataR8, PathToPalette, "4066", "4066", Path.Combine(PathToImages, "refaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4067", "4067", Path.Combine(PathToImages, "refhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4068", "4068", Path.Combine(PathToImages, "refoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4069", "4069", Path.Combine(PathToImages, "turreticon")}, // == 4070..4071
				new string[] {"--r8", PathToDataR8, PathToPalette, "4072", "4072", Path.Combine(PathToImages, "radaraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4072", "4072", Path.Combine(PathToImages, "radaraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4073", "4073", Path.Combine(PathToImages, "radarhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4074", "4074", Path.Combine(PathToImages, "radaroicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4075", "4075", Path.Combine(PathToImages, "rturreticon")}, // == 4076..4077
				new string[] {"--r8", PathToDataR8, PathToPalette, "4078", "4078", Path.Combine(PathToImages, "hightechaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4079", "4079", Path.Combine(PathToImages, "hightechhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4080", "4080", Path.Combine(PathToImages, "hightechoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4081", "4081", Path.Combine(PathToImages, "lightaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4082", "4082", Path.Combine(PathToImages, "lighthicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4083", "4083", Path.Combine(PathToImages, "lightoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4084", "4084", Path.Combine(PathToImages, "siloaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4085", "4085", Path.Combine(PathToImages, "silohicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4086", "4086", Path.Combine(PathToImages, "silooicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4087", "4087", Path.Combine(PathToImages, "heavyaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4088", "4088", Path.Combine(PathToImages, "heavyhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4089", "4089", Path.Combine(PathToImages, "heavyoicon")},
				// 4090 == orniicon
				// 4091 == heavyhicon
				new string[] {"--r8", PathToDataR8, PathToPalette, "4092", "4092", Path.Combine(PathToImages, "starportaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4093", "4093", Path.Combine(PathToImages, "starporthicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4094", "4094", Path.Combine(PathToImages, "starportoicon")},
				// 4095 = orniicon
				new string[] {"--r8", PathToDataR8, PathToPalette, "4096", "4096", Path.Combine(PathToImages, "repairaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4097", "4097", Path.Combine(PathToImages, "repairhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4098", "4098", Path.Combine(PathToImages, "repairoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4099", "4099", Path.Combine(PathToImages, "researchaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4100", "4100", Path.Combine(PathToImages, "researchhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4101", "4101", Path.Combine(PathToImages, "researchoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4102", "4102", Path.Combine(PathToImages, "palaceaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4103", "4103", Path.Combine(PathToImages, "palacehicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4104", "4104", Path.Combine(PathToImages, "palaceoicon")},
				// 4105 = orniicon
				// 4106..4107 = radaraicon
				// 4108 = conyardaicon
				new string[] {"--r8", PathToDataR8, PathToPalette, "4109", "4150", Path.Combine(PathToImages, "conmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4151", "4174", Path.Combine(PathToImages, "wtrpmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4175", "4194", Path.Combine(PathToImages, "barramake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4231", "4253", Path.Combine(PathToImages, "refmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4254", "4273", Path.Combine(PathToImages, "radarmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4274", "4294", Path.Combine(PathToImages, "highmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4295", "4312", Path.Combine(PathToImages, "lightmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4313", "4327", Path.Combine(PathToImages, "silomake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4328", "4346", Path.Combine(PathToImages, "heavymake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4347", "4369", Path.Combine(PathToImages, "starportmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4370", "4390", Path.Combine(PathToImages, "repairmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4391", "4412", Path.Combine(PathToImages, "researchmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4413", "4435", Path.Combine(PathToImages, "palacemake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4436", "4449", Path.Combine(PathToImages, "cranea"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4450", "4463", Path.Combine(PathToImages, "craneh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4463", "4477", Path.Combine(PathToImages, "craneo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4760", "4819", Path.Combine(PathToImages, "windtrap_anim"), "--building"}, //?
				new string[] {"--r8", PathToDataR8, PathToPalette, "4820", "4840", Path.Combine(PathToImages, "missile_launch"), "--building"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/MOUSE.R8"), PathToPalette, "0", "264", Path.Combine(PathToImages, "mouse"), "--transparent"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), PathToPalette, "0", "799", Path.Combine(PathToImages, "BASE"), "--tileset"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), PathToPalette, "748", "749", Path.Combine(PathToImages, "spice0")},
			};

			var SHPsToCreate = new string[][]
			{
				new string[] {"--shp", Path.Combine(PathToImages, "overlay.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "crates.png"), "32"},
				//new string[] {"--shp", Path.Combine(PathToImages, "shadow.png"), "32
				new string[] {"--shp", Path.Combine(PathToImages, "spicebloom.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "rockcrater1.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "rockcrater2.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "sandcrater1.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "sandcrater2.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "rifle.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rifledeath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rocket.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "fremen.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "sardaukar.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "engineer.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "thumper.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "missile.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "trike.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "quad.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "harvester.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "combata.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "siege.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "dmcv.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "sonic.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "combataturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "siegeturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "carryall.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "orni.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "combath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "devast.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "combathturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "deathhandmissile.png"), "24"},
				new string[] {"--shp", Path.Combine(PathToImages, "saboteur.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "deviator.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "raider.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "combato.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "combatoturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "frigate.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavya.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "radara.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "pwra.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "barra.png"), "80"},
				new string[] {"--shp", Path.Combine(PathToImages, "wall.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "conyarda.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "refa.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToImages, "hightecha.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "siloa.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "repaira.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "guntower.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "gunturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rockettower.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rocketturreta.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "researcha.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "starporta.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "lighta.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "palacea.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavyh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "radarh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "pwrh.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "barrh.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "conyardh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "refh.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToImages, "hightechh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "siloh.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "repairh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "gunturreth.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rocketturreth.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "researchh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "starporth.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "lighth.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "palaceh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavyo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "radaro.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "pwro.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "barro.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "conyardo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "refo.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToImages, "hightecho.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "siloo.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "repairo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "gunturreto.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rocketturreto.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "researcho.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "starporto.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "lighto.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "palaceo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "sandwormmouth.png"), "68"},
				new string[] {"--shp", Path.Combine(PathToImages, "sandwormdust.png"), "68"},
				new string[] {"--shp", Path.Combine(PathToImages, "wormsigns1.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToImages, "wormsigns2.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToImages, "wormsigns3.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToImages, "wormsigns4.png"), "16"},
				//new string[] {"--shp", Path.Combine(PathToImages, "sell.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "rifleicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "bazookaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "engineericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "thumpericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "sadaukaricon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "trikeicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "raidericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "quadicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "harvestericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "combataicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "combathicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "combatoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "mcvicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "missileicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "deviatoricon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "siegeicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "sonicicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "devasticon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "carryallicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "orniicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "fremenicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "saboteuricon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "deathhandicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "conyardaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "conyardhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "conyardoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "4plateicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "6plateicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "pwraicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "pwrhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "pwroicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "barraicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "barrhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "barroicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "wallicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "refaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "refhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "refoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "turreticon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "radaraicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "radarhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "radaroicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "rturreticon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "hightechaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "hightechhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "hightechoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "lightaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "lighthicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "lightoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "siloaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "silohicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "silooicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavyaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavyhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavyoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "starportaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "starporthicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "starportoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "repairaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "repairhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "repairoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "researchaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "researchhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "researchoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "palaceaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "palacehicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "palaceoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToImages, "conmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "wtrpmake.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToImages, "barramake.png"), "80"},
				new string[] {"--shp", Path.Combine(PathToImages, "refmake.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToImages, "radarmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "highmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "lightmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "silomake.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToImages, "heavymake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "starportmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "repairmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "researchmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "palacemake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "cranea.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "craneh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "craneo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "windtrap_anim.png"), "96"},																																																																																																																				                                   
				new string[] {"--shp", Path.Combine(PathToImages, "missile_launch.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToImages, "mouse.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToImages, "spice0.png"), "32"},
			};

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: "+s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			var t = new Thread( _ =>
			{
				try
				{
        			for (int i = 0; i < ExtractGameFiles.Length; i++)
					{
						progressBar.Percentage = i*100/ExtractGameFiles.Count();
						statusLabel.GetText = () => "Extracting...";
						Utility.Command.ConvertR8ToPng(ExtractGameFiles[i]);
					}

					for (int i = 0; i < SHPsToCreate.Length; i++)
					{
						progressBar.Percentage = i*100/SHPsToCreate.Count();
						statusLabel.GetText = () => "Converting...";
						Utility.Command.ConvertPngToShp(SHPsToCreate[i]);
						File.Delete(SHPsToCreate[i][1]);
					}

					File.Delete(Path.Combine(PathToImages, "BASE.tsx"));
					File.Copy("mods/d2k/tilesets/BASE.tsx", Path.Combine(PathToImages, "BASE.tsx"));
					// this is ugly: a GUI will open and close immediately after some delay
					Process.Start("OpenRA.TilesetBuilder.exe", Path.Combine(PathToImages, "BASE.png")+" 32 --export Content/d2k/Tilesets");
					File.Delete(Path.Combine(PathToImages, "BASE.tsx"));

					Game.RunAfterTick(() =>
					{
						progressBar.Percentage = 100;
						statusLabel.GetText = () => "Extraction and conversion complete.";
						backButton.IsDisabled = () => false;
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
