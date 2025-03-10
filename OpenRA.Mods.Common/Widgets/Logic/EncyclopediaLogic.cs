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
using System.Globalization;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class EncyclopediaLogic : ChromeLogic
	{
		[FluentReference("prerequisites")]
		const string Requires = "label-requires";

		readonly World world;
		readonly ModData modData;
		readonly Dictionary<ActorInfo, EncyclopediaInfo> info = new();

		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget titleLabel;
		readonly LabelWidget descriptionLabel;
		readonly SpriteFont descriptionFont;

		readonly ScrollPanelWidget actorList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;
		readonly ActorPreviewWidget previewWidget;

		readonly SpriteWidget portraitWidget;
		readonly Sprite portraitSprite;
		readonly Png defaultPortrait;

		readonly Widget productionContainer;
		readonly LabelWidget productionCost;
		readonly LabelWidget productionTime;
		readonly Widget productionPowerIcon;
		readonly LabelWidget productionPower;
		readonly List<Sheet> sheets = new();

		ActorInfo selectedActor;
		ScrollItemWidget firstItem;

		[ObjectCreator.UseCtor]
		public EncyclopediaLogic(Widget widget, World world, ModData modData, Action onExit)
		{
			this.world = world;
			this.modData = modData;

			actorList = widget.Get<ScrollPanelWidget>("ACTOR_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			widget.Get("ACTOR_INFO").IsVisible = () => selectedActor != null;

			previewWidget = widget.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
			previewWidget.IsVisible = () => selectedActor != null;

			descriptionPanel = widget.Get<ScrollPanelWidget>("ACTOR_DESCRIPTION_PANEL");
			titleLabel = descriptionPanel.GetOrNull<LabelWidget>("ACTOR_TITLE");
			descriptionLabel = descriptionPanel.Get<LabelWidget>("ACTOR_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[descriptionLabel.Font];

			portraitWidget = widget.GetOrNull<SpriteWidget>("ACTOR_PORTRAIT");
			if (portraitWidget != null)
			{
				defaultPortrait = new Png(modData.DefaultFileSystem.Open("encyclopedia/default.png"));
				var spriteBounds = new Rectangle(0, 0, defaultPortrait.Width, defaultPortrait.Height);
				var sheet = new Sheet(SheetType.BGRA, spriteBounds.Size.NextPowerOf2());
				sheets.Add(sheet);
				sheet.CreateBuffer();
				sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
				portraitSprite = new Sprite(sheet, spriteBounds, TextureChannel.RGBA);
				portraitWidget.GetSprite = () => portraitSprite;
			}

			actorList.RemoveChildren();

			productionContainer = descriptionPanel.GetOrNull("ACTOR_PRODUCTION");
			productionCost = productionContainer?.Get<LabelWidget>("COST");
			productionTime = productionContainer?.Get<LabelWidget>("TIME");
			productionPowerIcon = productionContainer?.Get("POWER_ICON");
			productionPower = productionContainer?.Get<LabelWidget>("POWER");

			foreach (var actor in modData.DefaultRules.Actors.Values)
			{
				if (actor.TraitInfos<IRenderActorPreviewSpritesInfo>().Count == 0)
					continue;

				var statistics = actor.TraitInfoOrDefault<UpdatesPlayerStatisticsInfo>();
				if (statistics != null && !string.IsNullOrEmpty(statistics.OverrideActor))
					continue;

				var encyclopedia = actor.TraitInfoOrDefault<EncyclopediaInfo>();
				if (encyclopedia == null)
					continue;

				info.Add(actor, encyclopedia);
			}

			var categories = info.Select(a => a.Value.Category).Distinct().
				OrderBy(string.IsNullOrWhiteSpace).ThenBy(s => s);

			foreach (var category in categories)
			{
				CreateActorGroup(category, info
					.Where(a => a.Value.Category == category)
					.OrderBy(a => a.Value.Order)
					.Select(a => a.Key));
			}

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		void CreateActorGroup(string title, IEnumerable<ActorInfo> actors)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => false, () => { });
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			actorList.AddChild(header);

			foreach (var actor in actors)
			{
				var item = ScrollItemWidget.Setup(template,
					() => selectedActor != null && selectedActor.Name == actor.Name,
					() => SelectActor(actor));

				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				var name = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault)?.Name;
				if (!string.IsNullOrEmpty(name))
					WidgetUtils.TruncateLabelToTooltip(label, FluentProvider.GetMessage(name));

				if (firstItem == null)
				{
					firstItem = item;
					SelectActor(actor);
				}

				actorList.AddChild(item);
			}
		}

		void SelectActor(ActorInfo actor)
		{
			var selectedInfo = info[actor];
			selectedActor = actor;

			Player previewOwner = null;
			if (!string.IsNullOrEmpty(selectedInfo.PreviewOwner))
				previewOwner = world.Players.FirstOrDefault(p => p.InternalName == selectedInfo.PreviewOwner);

			var typeDictionary = new TypeDictionary()
			{
				new OwnerInit(previewOwner ?? world.WorldActor.Owner),
				new FactionInit(world.WorldActor.Owner.PlayerReference.Faction)
			};

			foreach (var actorPreviewInit in actor.TraitInfos<IActorPreviewInitInfo>())
				foreach (var inits in actorPreviewInit.ActorPreviewInits(actor, ActorPreviewType.ColorPicker))
					typeDictionary.Add(inits);

			previewWidget.SetPreview(actor, typeDictionary);
			previewWidget.GetScale = () => selectedInfo.Scale;

			if (portraitWidget != null)
			{
				// PERF: Load individual portrait images directly, bypassing ChromeProvider,
				// to avoid stalls when loading a single large sheet.
				// Portrait images are required to all be the same size as the "default.png" image.
				var portrait = defaultPortrait;
				if (modData.DefaultFileSystem.TryOpen($"encyclopedia/{actor.Name}.png", out var s))
				{
					var p = new Png(s);
					if (p.Width == defaultPortrait.Width && p.Height == defaultPortrait.Height)
						portrait = p;
					else
					{
						Log.Write("debug", $"Failed to parse load portrait image for {actor.Name}.");
						Log.Write("debug", $"Expected size {defaultPortrait.Width}, {defaultPortrait.Height}, but found {p.Width}, {p.Height}.");
					}
				}

				OpenRA.Graphics.Util.FastCopyIntoSprite(portraitSprite, portrait);
				portraitSprite.Sheet.CommitBufferedData();
			}

			if (titleLabel != null)
				titleLabel.Text = ActorName(modData.DefaultRules, actor.Name);

			var bi = actor.TraitInfoOrDefault<BuildableInfo>();

			if (productionContainer != null && bi != null && !selectedInfo.HideBuildable)
			{
				productionContainer.Visible = true;
				var cost = actor.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

				var time = BuildTime(selectedActor, selectedInfo.BuildableQueue);
				productionTime.Text = WidgetUtils.FormatTime(time, world.Timestep);

				var costText = cost.ToString(NumberFormatInfo.CurrentInfo);
				productionCost.Text = costText;

				var power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
				if (power != 0)
				{
					productionPowerIcon.Visible = true;
					productionPower.Visible = true;
					productionPower.Text = power.ToString(NumberFormatInfo.CurrentInfo);
				}
				else
				{
					productionPowerIcon.Visible = false;
					productionPower.Visible = false;
				}
			}
			else if (productionContainer != null)
				productionContainer.Visible = false;

			var text = "";
			if (bi != null)
			{
				var prereqs = bi.Prerequisites
					.Select(a => ActorName(modData.DefaultRules, a))
					.Where(s => !s.StartsWith('~') && !s.StartsWith('!'))
					.ToList();

				if (prereqs.Count != 0)
					text += FluentProvider.GetMessage(Requires, "prerequisites", prereqs.JoinWith(", ")) + "\n\n";
			}

			if (selectedInfo != null && !string.IsNullOrEmpty(selectedInfo.Description))
				text += WidgetUtils.WrapText(FluentProvider.GetMessage(selectedInfo.Description), descriptionLabel.Bounds.Width, descriptionFont);

			var height = descriptionFont.Measure(text).Y;
			descriptionLabel.GetText = () => text;
			descriptionLabel.Bounds.Height = height;
			descriptionPanel.Layout.AdjustChildren();

			descriptionPanel.ScrollToTop();
		}

		static string ActorName(Ruleset rules, string name)
		{
			if (rules.Actors.TryGetValue(name.ToLowerInvariant(), out var actor))
			{
				var actorTooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return FluentProvider.GetMessage(actorTooltip.Name);
			}

			return name;
		}

		int BuildTime(ActorInfo info, string queue)
		{
			var bi = info.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				return 0;

			var time = bi.BuildDuration;
			if (time == -1)
			{
				var valued = info.TraitInfoOrDefault<ValuedInfo>();
				if (valued == null)
					return 0;
				else
					time = valued.Cost;
			}

			int pbi;
			if (queue != null)
			{
				var pqueue = modData.DefaultRules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>()
					.Where(x => x.Type == queue)).FirstOrDefault();

				pbi = pqueue?.BuildDurationModifier ?? 100;
			}
			else
			{
				var pqueue = modData.DefaultRules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>()
					.Where(x => bi.Queue.Contains(x.Type))).FirstOrDefault();

				pbi = pqueue?.BuildDurationModifier ?? 100;
			}

			time = time * bi.BuildDurationModifier * pbi / 10000;
			return time;
		}

		protected override void Dispose(bool disposing)
		{
			foreach (var sheet in sheets)
				sheet.Dispose();

			base.Dispose(disposing);
		}
	}
}
