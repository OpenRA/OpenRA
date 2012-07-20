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

			var PathToDataR8 = Path.Combine(Platform.SupportDir, "Content/d2k/DATA.R8");
			var PathToPalette = "mods/d2k/bits/d2k.pal";
			var PathToSHPs = Path.Combine(Platform.SupportDir, "Content/d2k/SHPs");
			var PathToTilesets = Path.Combine(Platform.SupportDir, "Content/d2k/Tilesets");

			var ExtractGameFiles = new string[][]
			{
				new string[] {"--r8", PathToDataR8, PathToPalette, "0", "2", Path.Combine(PathToSHPs, "overlay")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3", "3", Path.Combine(PathToSHPs, "repairing")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "15", "16", Path.Combine(PathToSHPs, "dots")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "17", "26", Path.Combine(PathToSHPs, "numbers")},
				//new string[] {"--r8", PathToDataR8, PathToPalette, "40", "101", Path.Combine(PathToSHPs, "shadow")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "102", "105", Path.Combine(PathToSHPs, "crates")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "107", "109", Path.Combine(PathToSHPs, "spicebloom")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "110", "111", Path.Combine(PathToSHPs, "stars")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "112", "112", Path.Combine(PathToSHPs, "greenuparrow")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "114", "129", Path.Combine(PathToSHPs, "rockcrater1")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "130", "145", Path.Combine(PathToSHPs, "rockcrater2")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "146", "161", Path.Combine(PathToSHPs, "sandcrater1")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "162", "177", Path.Combine(PathToSHPs, "sandcrater2")},
				// ?
				new string[] {"--r8", PathToDataR8, PathToPalette, "206", "381", Path.Combine(PathToSHPs, "rifle"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "382", "457", Path.Combine(PathToSHPs, "rifledeath"), "--infantrydeath"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "458", "693", Path.Combine(PathToSHPs, "bazooka"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "694", "929", Path.Combine(PathToSHPs, "fremen"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "930", "1165", Path.Combine(PathToSHPs, "sardaukar"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1166", "1221", Path.Combine(PathToSHPs, "engineer"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1342", "1401", Path.Combine(PathToSHPs, "engineerdeath"), "--infantrydeath"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1402", "1502", Path.Combine(PathToSHPs, "thumper"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1543", "1602", Path.Combine(PathToSHPs, "thumperdeath"), "--infantrydeath"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1603", "1634", Path.Combine(PathToSHPs, "missiletank"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1635", "1666", Path.Combine(PathToSHPs, "trike"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1667", "1698", Path.Combine(PathToSHPs, "quad"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1699", "1730", Path.Combine(PathToSHPs, "harvester"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1731", "1762", Path.Combine(PathToSHPs, "combata"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1763", "1794", Path.Combine(PathToSHPs, "siegetank"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1795", "1826", Path.Combine(PathToSHPs, "dmcv"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1827", "1858", Path.Combine(PathToSHPs, "sonictank"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1859", "1890", Path.Combine(PathToSHPs, "combataturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1891", "1922", Path.Combine(PathToSHPs, "siegeturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1923", "1954", Path.Combine(PathToSHPs, "carryall"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "1955", "2050", Path.Combine(PathToSHPs, "orni"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2051", "2082", Path.Combine(PathToSHPs, "combath"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2083", "2114", Path.Combine(PathToSHPs, "devast"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2115", "2146", Path.Combine(PathToSHPs, "combathturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2147", "2148", Path.Combine(PathToSHPs, "deathhandmissile")},

				new string[] {"--r8", PathToDataR8, PathToPalette, "2245", "2284", Path.Combine(PathToSHPs, "saboteur"), "--infantry"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2325", "2388", Path.Combine(PathToSHPs, "saboteurdeath"), "--infantrydeath"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2389", "2420", Path.Combine(PathToSHPs, "deviatortank"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2421", "2452", Path.Combine(PathToSHPs, "raider"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2453", "2484", Path.Combine(PathToSHPs, "combato"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2485", "2516", Path.Combine(PathToSHPs, "combatoturret"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2517", "2517", Path.Combine(PathToSHPs, "frigate"), "--vehicle"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2518", "2520", Path.Combine(PathToSHPs, "heavya"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2521", "2522", Path.Combine(PathToSHPs, "radara"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2523", "2524", Path.Combine(PathToSHPs, "pwra"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2525", "2526", Path.Combine(PathToSHPs, "barra"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2527", "2558", Path.Combine(PathToSHPs, "walla"), "--wall"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2559", "2560", Path.Combine(PathToSHPs, "conyarda"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2561", "2563", Path.Combine(PathToSHPs, "refa"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2564", "2565", Path.Combine(PathToSHPs, "hightecha"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2566", "2570", Path.Combine(PathToSHPs, "siloa"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2571", "2572", Path.Combine(PathToSHPs, "repaira"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2573", "2588", Path.Combine(PathToSHPs, "guntowera"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2589", "2620", Path.Combine(PathToSHPs, "gunturreta"), "--turret"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2621", "2636", Path.Combine(PathToSHPs, "rockettowera"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2637", "2668", Path.Combine(PathToSHPs, "rocketturreta"), "--turret"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2669", "2670", Path.Combine(PathToSHPs, "researcha"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2671", "2672", Path.Combine(PathToSHPs, "starporta"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2673", "2675", Path.Combine(PathToSHPs, "lighta"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2676", "2677", Path.Combine(PathToSHPs, "palacea"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2678", "2680", Path.Combine(PathToSHPs, "heavyh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2681", "2682", Path.Combine(PathToSHPs, "radarh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2683", "2684", Path.Combine(PathToSHPs, "pwrh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2685", "2686", Path.Combine(PathToSHPs, "barrh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2687", "2718", Path.Combine(PathToSHPs, "wallh"), "--wall"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2719", "2720", Path.Combine(PathToSHPs, "conyardh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2721", "2723", Path.Combine(PathToSHPs, "refh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2724", "2725", Path.Combine(PathToSHPs, "hightechh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2726", "2730", Path.Combine(PathToSHPs, "siloh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2731", "2732", Path.Combine(PathToSHPs, "repairh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2733", "2748", Path.Combine(PathToSHPs, "guntowerh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2749", "2780", Path.Combine(PathToSHPs, "gunturreth"), "--turret"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2781", "2796", Path.Combine(PathToSHPs, "rockettowerh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2797", "2828", Path.Combine(PathToSHPs, "rocketturreth"), "--turret"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2829", "2830", Path.Combine(PathToSHPs, "researchh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2831", "2832", Path.Combine(PathToSHPs, "starporth"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2833", "2835", Path.Combine(PathToSHPs, "lighth"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2836", "2837", Path.Combine(PathToSHPs, "palaceh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2838", "2840", Path.Combine(PathToSHPs, "heavyo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2841", "2842", Path.Combine(PathToSHPs, "radaro"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2843", "2844", Path.Combine(PathToSHPs, "pwro"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2845", "2846", Path.Combine(PathToSHPs, "barro"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2847", "2878", Path.Combine(PathToSHPs, "wallo"), "--wall"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2879", "2880", Path.Combine(PathToSHPs, "conyardo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2881", "2883", Path.Combine(PathToSHPs, "refo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2884", "2885", Path.Combine(PathToSHPs, "hightecho"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2886", "2890", Path.Combine(PathToSHPs, "siloo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2891", "2892", Path.Combine(PathToSHPs, "repairo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2893", "2908", Path.Combine(PathToSHPs, "guntowero"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2909", "2940", Path.Combine(PathToSHPs, "gunturreto"), "--turret"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2941", "2956", Path.Combine(PathToSHPs, "rockettowero"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2957", "2988", Path.Combine(PathToSHPs, "rocketturreto"), "--turret"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2989", "2990", Path.Combine(PathToSHPs, "researcho"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2991", "2992", Path.Combine(PathToSHPs, "starporto"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2993", "2995", Path.Combine(PathToSHPs, "lighto"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2996", "2997", Path.Combine(PathToSHPs, "palaceo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2998", "2998", Path.Combine(PathToSHPs, "sietch"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "2999", "3000", Path.Combine(PathToSHPs, "starportc"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3001", "3003", Path.Combine(PathToSHPs, "heavyc"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3004", "3005", Path.Combine(PathToSHPs, "palacec"), "--building"},
				//conyardc repetition
				new string[] {"--r8", PathToDataR8, PathToPalette, "3008", "3013", Path.Combine(PathToSHPs, "plates")},
				//projectiles
				new string[] {"--r8", PathToDataR8, PathToPalette, "3370", "3380", Path.Combine(PathToSHPs, "unload"), "--projectile"},
				//explosions
				new string[] {"--r8", PathToDataR8, PathToPalette, "3549", "3564", Path.Combine(PathToSHPs, "wormjaw")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3565", "3585", Path.Combine(PathToSHPs, "wormdust")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3586", "3600", Path.Combine(PathToSHPs, "wormsigns1")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3601", "3610", Path.Combine(PathToSHPs, "wormsigns2")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3611", "3615", Path.Combine(PathToSHPs, "wormsigns3")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "3616", "3620", Path.Combine(PathToSHPs, "wormsigns4")},
				//new string[] {"--r8", PathToDataR8, PathToPalette, "3679", "3686", "sell"},
				//explosions and muzzle flash
				new string[] {"--r8", PathToDataR8, PathToPalette, "4011", "4011", Path.Combine(PathToSHPs, "rifleicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4012", "4012", Path.Combine(PathToSHPs, "bazookaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4013", "4013", Path.Combine(PathToSHPs, "engineericon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4014", "4014", Path.Combine(PathToSHPs, "thumpericon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4015", "4015", Path.Combine(PathToSHPs, "sardaukaricon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4016", "4016", Path.Combine(PathToSHPs, "trikeicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4017", "4017", Path.Combine(PathToSHPs, "raidericon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4018", "4018", Path.Combine(PathToSHPs, "quadicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4019", "4019", Path.Combine(PathToSHPs, "harvestericon")}, // == 4044
				new string[] {"--r8", PathToDataR8, PathToPalette, "4020", "4020", Path.Combine(PathToSHPs, "combataicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4021", "4021", Path.Combine(PathToSHPs, "combathicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4022", "4022", Path.Combine(PathToSHPs, "combatoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4023", "4023", Path.Combine(PathToSHPs, "mcvicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4024", "4024", Path.Combine(PathToSHPs, "missiletankicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4025", "4025", Path.Combine(PathToSHPs, "deviatortankicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4026", "4026", Path.Combine(PathToSHPs, "siegetankicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4027", "4027", Path.Combine(PathToSHPs, "sonictankicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4028", "4028", Path.Combine(PathToSHPs, "devasticon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4029", "4029", Path.Combine(PathToSHPs, "carryallicon")}, // == 4030
				new string[] {"--r8", PathToDataR8, PathToPalette, "4031", "4031", Path.Combine(PathToSHPs, "orniicon")}, // == 4062
				new string[] {"--r8", PathToDataR8, PathToPalette, "4032", "4032", Path.Combine(PathToSHPs, "fremenicon")}, // == 4033
				new string[] {"--r8", PathToDataR8, PathToPalette, "4034", "4034", Path.Combine(PathToSHPs, "saboteuricon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4035", "4035", Path.Combine(PathToSHPs, "deathhandicon")},
				// "4036..4045 = repetitions
				new string[] {"--r8", PathToDataR8, PathToPalette, "4046", "4046", Path.Combine(PathToSHPs, "conyardaicon")}, // == 4049
				new string[] {"--r8", PathToDataR8, PathToPalette, "4047", "4047", Path.Combine(PathToSHPs, "conyardhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4048", "4048", Path.Combine(PathToSHPs, "conyardoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4050", "4050", Path.Combine(PathToSHPs, "4plateicon")}, // == 4051..4052
				new string[] {"--r8", PathToDataR8, PathToPalette, "4053", "4053", Path.Combine(PathToSHPs, "6plateicon")}, // == 4054..4055
				new string[] {"--r8", PathToDataR8, PathToPalette, "4056", "4056", Path.Combine(PathToSHPs, "pwraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4057", "4057", Path.Combine(PathToSHPs, "pwrhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4058", "4058", Path.Combine(PathToSHPs, "pwroicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4059", "4059", Path.Combine(PathToSHPs, "barraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4060", "4060", Path.Combine(PathToSHPs, "barrhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4061", "4061", Path.Combine(PathToSHPs, "barroicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4063", "4063", Path.Combine(PathToSHPs, "wallicon")}, // == 4061..4062
				new string[] {"--r8", PathToDataR8, PathToPalette, "4066", "4066", Path.Combine(PathToSHPs, "refaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4067", "4067", Path.Combine(PathToSHPs, "refhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4068", "4068", Path.Combine(PathToSHPs, "refoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4069", "4069", Path.Combine(PathToSHPs, "turreticon")}, // == 4070..4071
				new string[] {"--r8", PathToDataR8, PathToPalette, "4072", "4072", Path.Combine(PathToSHPs, "radaraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4072", "4072", Path.Combine(PathToSHPs, "radaraicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4073", "4073", Path.Combine(PathToSHPs, "radarhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4074", "4074", Path.Combine(PathToSHPs, "radaroicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4075", "4075", Path.Combine(PathToSHPs, "rturreticon")}, // == 4076..4077
				new string[] {"--r8", PathToDataR8, PathToPalette, "4078", "4078", Path.Combine(PathToSHPs, "hightechaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4079", "4079", Path.Combine(PathToSHPs, "hightechhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4080", "4080", Path.Combine(PathToSHPs, "hightechoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4081", "4081", Path.Combine(PathToSHPs, "lightaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4082", "4082", Path.Combine(PathToSHPs, "lighthicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4083", "4083", Path.Combine(PathToSHPs, "lightoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4084", "4084", Path.Combine(PathToSHPs, "siloaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4085", "4085", Path.Combine(PathToSHPs, "silohicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4086", "4086", Path.Combine(PathToSHPs, "silooicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4087", "4087", Path.Combine(PathToSHPs, "heavyaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4088", "4088", Path.Combine(PathToSHPs, "heavyhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4089", "4089", Path.Combine(PathToSHPs, "heavyoicon")},
				// 4090 == orniicon
				// 4091 == heavyhicon
				new string[] {"--r8", PathToDataR8, PathToPalette, "4092", "4092", Path.Combine(PathToSHPs, "starportaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4093", "4093", Path.Combine(PathToSHPs, "starporthicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4094", "4094", Path.Combine(PathToSHPs, "starportoicon")},
				// 4095 = orniicon
				new string[] {"--r8", PathToDataR8, PathToPalette, "4096", "4096", Path.Combine(PathToSHPs, "repairaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4097", "4097", Path.Combine(PathToSHPs, "repairhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4098", "4098", Path.Combine(PathToSHPs, "repairoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4099", "4099", Path.Combine(PathToSHPs, "researchaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4100", "4100", Path.Combine(PathToSHPs, "researchhicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4101", "4101", Path.Combine(PathToSHPs, "researchoicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4102", "4102", Path.Combine(PathToSHPs, "palaceaicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4103", "4103", Path.Combine(PathToSHPs, "palacehicon")},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4104", "4104", Path.Combine(PathToSHPs, "palaceoicon")},
				// 4105 = orniicon
				// 4106..4107 = radaraicon
				// 4108 = conyardaicon
				new string[] {"--r8", PathToDataR8, PathToPalette, "4109", "4150", Path.Combine(PathToSHPs, "conmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4151", "4174", Path.Combine(PathToSHPs, "wtrpmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4175", "4194", Path.Combine(PathToSHPs, "barramake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4231", "4253", Path.Combine(PathToSHPs, "refmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4254", "4273", Path.Combine(PathToSHPs, "radarmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4274", "4294", Path.Combine(PathToSHPs, "highmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4295", "4312", Path.Combine(PathToSHPs, "lightmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4313", "4327", Path.Combine(PathToSHPs, "silomake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4328", "4346", Path.Combine(PathToSHPs, "heavymake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4347", "4369", Path.Combine(PathToSHPs, "starportmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4370", "4390", Path.Combine(PathToSHPs, "repairmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4391", "4412", Path.Combine(PathToSHPs, "researchmake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4413", "4435", Path.Combine(PathToSHPs, "palacemake"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4436", "4449", Path.Combine(PathToSHPs, "cranea"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4450", "4463", Path.Combine(PathToSHPs, "craneh"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4463", "4477", Path.Combine(PathToSHPs, "craneo"), "--building"},
				new string[] {"--r8", PathToDataR8, PathToPalette, "4760", "4819", Path.Combine(PathToSHPs, "windtrap_anim"), "--building"}, //?
				new string[] {"--r8", PathToDataR8, PathToPalette, "4820", "4840", Path.Combine(PathToSHPs, "missile_launch"), "--building"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/MOUSE.R8"), PathToPalette, "0", "264", Path.Combine(PathToSHPs, "mouse")},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "BASE"), "--tileset"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBASE.R8"), PathToPalette, "748", "749", Path.Combine(PathToSHPs, "spice0")},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBAT.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "BAT"), "--tileset"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXBGBS.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "BGBS"), "--tileset"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXICE.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "ICE"), "--tileset"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXTREE.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "TREE"), "--tileset"},
				new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXWAST.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "WAST"), "--tileset"},
				//new string[] {"--r8", Path.Combine(Platform.SupportDir, "Content/d2k/BLOXXMAS.R8"), PathToPalette, "0", "799", Path.Combine(PathToTilesets, "XMAS"), "--tileset"},
			};

			var SHPsToCreate = new string[][]
			{
				new string[] {"--shp", Path.Combine(PathToSHPs, "overlay.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairing.png"), "24"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "numbers.png"), "8"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "dots.png"), "4"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "crates.png"), "32"},
				//new string[] {"--shp", Path.Combine(PathToSHPs, "shadow.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "spicebloom.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "stars.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "greenuparrow.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rockcrater1.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rockcrater2.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sandcrater1.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sandcrater2.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rifle.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rifledeath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "bazooka.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "fremen.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sardaukar.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "engineer.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "engineerdeath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "thumper.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "thumperdeath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "missiletank.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "trike.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "quad.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "harvester.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combata.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siegetank.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "dmcv.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sonictank.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combataturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siegeturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "carryall.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "orni.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "devast.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combathturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "deathhandmissile.png"), "24"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "saboteur.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "saboteurdeath.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "deviatortank.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "raider.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combato.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combatoturret.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "frigate.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavya.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radara.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "pwra.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barra.png"), "80"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "walla.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conyarda.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refa.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "hightecha.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siloa.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repaira.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "guntowera.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "gunturreta.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rockettowera.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rocketturreta.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researcha.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starporta.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lighta.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palacea.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavyh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radarh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "pwrh.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wallh.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barrh.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conyardh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refh.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "hightechh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siloh.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "guntowerh.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "gunturreth.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rockettowerh.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rocketturreth.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researchh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starporth.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lighth.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palaceh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavyo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radaro.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "pwro.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barro.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wallo.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conyardo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refo.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "hightecho.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siloo.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "guntowero.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "gunturreto.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rockettowero.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rocketturreto.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researcho.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starporto.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lighto.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palaceo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "unload.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wormjaw.png"), "68"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wormdust.png"), "68"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wormsigns1.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wormsigns2.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wormsigns3.png"), "16"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wormsigns4.png"), "16"},
				//new string[] {"--shp", Path.Combine(PathToSHPs, "sell.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rifleicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "bazookaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "engineericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "thumpericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sardaukaricon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "trikeicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "raidericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "quadicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "harvestericon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combataicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combathicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "combatoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "mcvicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "missiletankicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "deviatortankicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siegetankicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sonictankicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "devasticon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "carryallicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "orniicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "fremenicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "saboteuricon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "deathhandicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conyardaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conyardhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conyardoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "4plateicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "6plateicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "pwraicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "pwrhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "pwroicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barraicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barrhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barroicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wallicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "turreticon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radaraicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radarhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radaroicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "rturreticon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "hightechaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "hightechhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "hightechoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lightaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lighthicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lightoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "siloaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "silohicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "silooicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavyaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavyhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavyoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starportaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starporthicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starportoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researchaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researchhicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researchoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palaceaicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palacehicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palaceoicon.png"), "60"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "conmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "wtrpmake.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "barramake.png"), "80"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "refmake.png"), "120"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "radarmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "highmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "lightmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "silomake.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavymake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starportmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "repairmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "researchmake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palacemake.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "cranea.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "craneh.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "craneo.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "windtrap_anim.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "missile_launch.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "mouse.png"), "48"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "spice0.png"), "32"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "sietch.png"), "64"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "starportc.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "heavyc.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "palacec.png"), "96"},
				new string[] {"--shp", Path.Combine(PathToSHPs, "plates.png"), "32"},
			};

			var SHPsToTranspose = new string[][]
			{
				new string[] {"--transpose", Path.Combine(PathToSHPs, "orni.shp"), Path.Combine(PathToSHPs, "orni.shp"), "0", "32", "3"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "rifle.shp"), Path.Combine(PathToSHPs, "rifle.shp"), "8", "8", "6", "56", "8", "5", "112", "8", "3", "136", "8", "5"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "bazooka.shp"), Path.Combine(PathToSHPs, "bazooka.shp"), "8", "8", "6", "56", "8", "5", "112", "8", "3", "136", "8", "5"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "fremen.shp"), Path.Combine(PathToSHPs, "fremen.shp"), "8", "8", "6", "56", "8", "5", "112", "8", "3", "136", "8", "5"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "sardaukar.shp"), Path.Combine(PathToSHPs, "sardaukar.shp"), "8", "8", "6", "56", "8", "5", "112", "8", "3", "136", "8", "5"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "thumper.shp"), Path.Combine(PathToSHPs, "thumper.shp"), "8", "8", "6"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "engineer.shp"), Path.Combine(PathToSHPs, "engineer.shp"), "8", "8", "6"},
				new string[] {"--transpose", Path.Combine(PathToSHPs, "saboteur.shp"), Path.Combine(PathToSHPs, "saboteur.shp"), "8", "8", "4"},
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

					for (int i = 0; i < SHPsToTranspose.Length; i++)
					{
						progressBar.Percentage = i*100/SHPsToTranspose.Count();
						statusLabel.GetText = () => "Transposing...";
						Utility.Command.TransposeShp(SHPsToTranspose[i]);
					}

					statusLabel.GetText = () => "Building tilesets...";
					int c = 0;
					string[] TilesetArray = new string[] { "BASE", "BAT", "BGBS", "ICE", "TREE", "WAST" };
					foreach (string set in TilesetArray)
					{
						progressBar.Percentage = c*100/TilesetArray.Count();
						File.Delete(Path.Combine(PathToTilesets, "{0}.tsx".F(set)));
						File.Copy("mods/d2k/tilesets/{0}.tsx".F(set), Path.Combine(PathToTilesets, "{0}.tsx".F(set)));
						// this is ugly: a GUI will open and close immediately after some delay
						Process p = new Process();
						ProcessStartInfo TilesetBuilderProcessStartInfo = new ProcessStartInfo("OpenRA.TilesetBuilder.exe", Path.Combine(PathToTilesets, "{0}.png".F(set))+" 32 --export Content/d2k/Tilesets");
						p.StartInfo = TilesetBuilderProcessStartInfo;
						p.Start();
						p.WaitForExit();
						File.Delete(Path.Combine(PathToTilesets, "{0}.tsx".F(set)));
						File.Delete(Path.Combine(PathToTilesets, "{0}.png".F(set)));
						File.Delete(Path.Combine(PathToTilesets, "{0}.yaml".F(set.ToLower())));
						File.Delete(Path.Combine(PathToTilesets, "{0}.pal".F(set.ToLower())));
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
