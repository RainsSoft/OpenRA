#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorSelectorLogic
	{
		readonly EditorViewportControllerWidget editor;
		readonly DropDownButtonWidget ownersDropDown;
		readonly ScrollPanelWidget panel;
		readonly ScrollItemWidget itemTemplate;
		readonly Ruleset modRules;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		PlayerReference selectedOwner;

		[ObjectCreator.UseCtor]
		public ActorSelectorLogic(Widget widget, World world, WorldRenderer worldRenderer, Ruleset modRules)
		{
			this.modRules = modRules;
			this.world = world;
			this.worldRenderer = worldRenderer;

			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			ownersDropDown = widget.Get<DropDownButtonWidget>("OWNERS_DROPDOWN");

			panel = widget.Get<ScrollPanelWidget>("ACTORTEMPLATE_LIST");
			itemTemplate = panel.Get<ScrollItemWidget>("ACTORPREVIEW_TEMPLATE");
			panel.Layout = new GridLayout(panel);

			var editorLayer = world.WorldActor.Trait<EditorActorLayer>();

			selectedOwner = editorLayer.Players.Players.Values.First();
			Func<PlayerReference, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () =>
				{
					selectedOwner = option;

					ownersDropDown.Text = selectedOwner.Name;
					ownersDropDown.TextColor = selectedOwner.Color.RGB;

					IntializeActorPreviews();
				});

				item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
				item.GetColor = () => option.Color.RGB;

				return item;
			};

			ownersDropDown.OnClick = () =>
			{
				var owners = editorLayer.Players.Players.Values.OrderBy(p => p.Name);
				ownersDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, setupItem);
			};

			ownersDropDown.Text = selectedOwner.Name;
			ownersDropDown.TextColor = selectedOwner.Color.RGB;

			IntializeActorPreviews();
		}

		void IntializeActorPreviews()
		{
			panel.RemoveChildren();

			var actors = modRules.Actors.Where(a => !a.Value.Name.Contains('^'))
				.Select(a => a.Value);

			foreach (var a in actors)
			{
				var actor = a;
				if (actor.Traits.Contains<BridgeInfo>()) // bridge layer takes care about that automatically
					continue;

				if (!actor.Traits.Contains<IRenderActorPreviewInfo>())
					continue;

				var filter = actor.Traits.GetOrDefault<EditorTilesetFilterInfo>();
				if (filter != null)
				{
					if (filter.ExcludeTilesets != null && filter.ExcludeTilesets.Contains(world.TileSet.Id))
						continue;
					if (filter.RequireTilesets != null && !filter.RequireTilesets.Contains(world.TileSet.Id))
						continue;
				}

				var td = new TypeDictionary();
				td.Add(new FacingInit(92));
				td.Add(new TurretFacingInit(92));
				td.Add(new HideBibPreviewInit());
				td.Add(new OwnerInit(selectedOwner.Name));
				td.Add(new FactionInit(selectedOwner.Faction));

				try
				{
					var item = ScrollItemWidget.Setup(itemTemplate,
						() => { var brush = editor.CurrentBrush as EditorActorBrush; return brush != null && brush.Actor == actor; },
						() => editor.SetBrush(new EditorActorBrush(editor, actor, selectedOwner, worldRenderer)));

					var preview = item.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
					preview.SetPreview(actor, td);

					// Scale templates to fit within the panel
					var scale = 1f;
					if (scale * preview.IdealPreviewSize.X > itemTemplate.Bounds.Width)
						scale = (float)(itemTemplate.Bounds.Width - panel.ItemSpacing) / (float)preview.IdealPreviewSize.X;

					preview.GetScale = () => scale;
					preview.Bounds.Width = (int)(scale * preview.IdealPreviewSize.X);
					preview.Bounds.Height = (int)(scale * preview.IdealPreviewSize.Y);

					item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
					item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
					item.IsVisible = () => true;

					var tooltip = actor.Traits.GetOrDefault<TooltipInfo>();
					item.GetTooltipText = () => tooltip == null ? actor.Name : tooltip.Name + " (" + actor.Name + ")";

					panel.AddChild(item);
				}
				catch
				{
					Log.Write("debug", "Map editor ignoring actor {0}, because of missing sprites for tileset {1}.",
						actor.Name, world.TileSet.Id);
					continue;
				}
			}
		}
	}
}
