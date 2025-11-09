using System;
using System.Linq;
using System.Reflection;
using OpenRA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class ClassicProductionLogicCA : ChromeLogic
	{
		const string PromotionGroupName = "Promotion";
		const string CommanderTreeOverlayId = "COMMANDER_TREE_OVERLAY";

		readonly ProductionPaletteWidget palette;
		readonly World world;

		void SetupProductionGroupButton(ProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			var queues = world?.LocalPlayer?.PlayerActor?
				.TraitsImplementing<ProductionQueue>()
				.Where(q => (q.Info.Group ?? q.Info.Type) == button.ProductionGroup)
				.ToArray() ?? Array.Empty<ProductionQueue>();

			if (string.Equals(button.ProductionGroup, PromotionGroupName, StringComparison.OrdinalIgnoreCase))
			{
				SetupPromotionButton(button, queues);
				return;
			}

			void SelectTab(bool reverse)
			{
				palette.CurrentQueue = queues.FirstOrDefault(q => q.Enabled);

				// When a tab is selected, scroll to the top because the current row position may be invalid for the new tab
				palette.ScrollToTop();

				// Attempt to pick up a completed building (if there is one) so it can be placed
				palette.PickUpCompletedBuilding();
			}

			button.IsDisabled = () => !queues.Any(q => q.AnyItemsToBuild() || q.AlwaysVisible);
			button.OnMouseUp = mi => SelectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => SelectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.OnClick = () => SelectTab(false);
			button.IsHighlighted = () => queues.Contains(palette.CurrentQueue);

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName + "-disabled" :
				queues.Any(q => q.AllQueued().Any(i => i.Done)) ? chromeName + "-alert" : chromeName;
		}

		void SetupPromotionButton(ProductionTypeButtonWidget button, ProductionQueue[] queues)
		{
			button.IsDisabled = () => !queues.Any(q => q.Enabled);

			button.OnMouseDown = mi =>
			{
				if (mi.Button == MouseButton.Left && !button.IsDisabled())
					OpenCommanderTree();
			};
			button.OnMouseUp = _ => { };
			button.OnClick = () => { };
			button.OnKeyPress = e =>
			{
				if (e.Event != KeyInputEvent.Down || button.IsDisabled())
					return;

				OpenCommanderTree();
			};
			button.IsHighlighted = () =>
			{
				var current = Ui.CurrentWindow();
				return current != null && current.Id == CommanderTreeOverlayId;
			};

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () =>
			{
				if (button.IsDisabled())
					return chromeName + "-disabled";

				return queues.Any(q => q.AllQueued().Any(i => i.Done)) ? chromeName + "-alert" : chromeName;
			};
		}

		void OpenCommanderTree()
		{
			var current = Ui.CurrentWindow();
			if (current != null && current.Id == CommanderTreeOverlayId)
			{
				Ui.CloseWindow();
				return;
			}

			Game.OpenWindow(world, CommanderTreeOverlayId);
			Ui.ResetTooltips();
		}

		[ObjectCreator.UseCtor]
		public ClassicProductionLogicCA(Widget widget, World world)
		{
			this.world = world;
			palette = widget.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");
			var tooltipField = typeof(ProductionPaletteWidget).GetField("TooltipTemplate", BindingFlags.Instance | BindingFlags.Public);
			if (tooltipField != null)
			{
				try
				{
					tooltipField.SetValue(palette, "CA_PRODUCTION_TOOLTIP");
					Log.Write("debug", $"ClassicProductionLogicCA palette template={palette.TooltipTemplate}");
				}
				catch (FieldAccessException)
				{
				}
			}

			var background = widget.GetOrNull("PALETTE_BACKGROUND");
			var foreground = widget.GetOrNull("PALETTE_FOREGROUND");
			if (background != null || foreground != null)
			{
				Widget backgroundTemplate = null;
				Widget backgroundBottom = null;
				Widget foregroundTemplate = null;

				if (background != null)
				{
					backgroundTemplate = background.Get("ROW_TEMPLATE");
					backgroundBottom = background.GetOrNull("BOTTOM_CAP");
				}

				if (foreground != null)
					foregroundTemplate = foreground.Get("ROW_TEMPLATE");

				void UpdateBackground(int _, int icons)
				{
					var rows = Math.Max(palette.MinimumRows, (icons + palette.Columns - 1) / palette.Columns);
					rows = Math.Min(rows, palette.MaximumRows);

					if (background != null)
					{
						background.RemoveChildren();

						var rowHeight = backgroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = backgroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							background.AddChild(row);
						}

						if (backgroundBottom == null)
							return;

						backgroundBottom.Bounds.Y = rows * rowHeight;
						background.AddChild(backgroundBottom);
					}

					if (foreground != null)
					{
						foreground.RemoveChildren();

						var rowHeight = foregroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = foregroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							foreground.AddChild(row);
						}
					}
				}

				palette.OnIconCountChanged += UpdateBackground;

				// Set the initial palette state
				UpdateBackground(0, 0);
			}

			var typesContainer = widget.Get("PRODUCTION_TYPES");
			foreach (var i in typesContainer.Children)
				SetupProductionGroupButton(i as ProductionTypeButtonWidget);

			var ticker = widget.Get<LogicTickerWidget>("PRODUCTION_TICKER");
			ticker.OnTick = () =>
			{
				if (palette.CurrentQueue == null || palette.DisplayedIconCount == 0)
				{
					// Select the first active tab
					foreach (var b in typesContainer.Children)
					{
						if (b is not ProductionTypeButtonWidget button || button.IsDisabled())
							continue;

						button.OnClick();
						break;
					}
				}
			};

			var scrollDown = widget.GetOrNull<ButtonWidget>("SCROLL_DOWN_BUTTON");

			if (scrollDown != null)
			{
				scrollDown.OnClick = palette.ScrollDown;
				scrollDown.IsVisible = () => palette.TotalIconCount > palette.MaxIconRowOffset * palette.Columns;
				scrollDown.IsDisabled = () => !palette.CanScrollDown;
			}

			var scrollUp = widget.GetOrNull<ButtonWidget>("SCROLL_UP_BUTTON");

			if (scrollUp != null)
			{
				scrollUp.OnClick = palette.ScrollUp;
				scrollUp.IsVisible = () => palette.TotalIconCount > palette.MaxIconRowOffset * palette.Columns;
				scrollUp.IsDisabled = () => !palette.CanScrollUp;
			}

			SetMaximumVisibleRows(palette);
		}

		static void SetMaximumVisibleRows(ProductionPaletteWidget productionPalette)
		{
			var screenHeight = Game.Renderer.Resolution.Height;

			// Get height of currently displayed icons
			var containerWidget = Ui.Root.GetOrNull<ContainerWidget>("SIDEBAR_PRODUCTION");

			if (containerWidget == null)
				return;

			var sidebarProductionHeight = containerWidget.Bounds.Y;

			// Check if icon heights exceed y resolution
			var maxItemsHeight = screenHeight - sidebarProductionHeight;

			var maxIconRowOffest = maxItemsHeight / productionPalette.IconSize.Y - 1;
			productionPalette.MaxIconRowOffset = Math.Min(maxIconRowOffest, productionPalette.MaximumRows);
		}
	}
}
