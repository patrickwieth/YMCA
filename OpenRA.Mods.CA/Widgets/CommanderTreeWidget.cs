using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class CommanderTreeWidget : Widget
	{
		const string PromotionQueueType = "Promotions";
		const string PromotionGroup = "Promotion";

		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly string clockAnimation = "clock";
		readonly string clockSequence = "idle";
		readonly string notBuildableAnimation = "clock";
		readonly string notBuildableSequence = "idle";
		readonly string clockPalette = "chrome";
		readonly string notBuildablePalette = "chrome";
		readonly string clickSound = ChromeMetrics.Get<string>("ClickSound");
		readonly string clickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
		readonly Color overlayTextColor = Color.White;
		readonly Color readyTextAltColor = Color.Gold;
		readonly string infiniteSymbol = "\u221E";
		readonly float2 queuedOffset = new(4f, 2f);

		Animation clock;
		Animation cantBuild;

		float2 iconOffset;
		SpriteFont overlayFont;
		SpriteFont symbolFont;
		string readyText;
		string holdText;
		float2 readyOffset;
		float2 holdOffset;
		float2 infiniteOffset;

		ProductionIcon tooltipIcon;

		readonly Dictionary<ActorInfo, CommanderNode> nodes = new();
		readonly List<CommanderEdge> edges = new();

		readonly List<CommanderGroup> promotionGroups = new();

		ProductionQueue promotionsQueue;

		readonly List<ProductionItem> sharedQueuedItems = new();
		readonly HashSet<ActorInfo> visibleActors = new();
		Player cachedQueueOwner;
		IProductionIconOverlay[] iconOverlays = Array.Empty<IProductionIconOverlay>();

		int contentWidth;
		int contentHeight;
		bool layoutDirty = true;

		CommanderNode hoverNode;
		CommanderNode lastTooltipNode;
		int currentTooltipToken;
		bool commanderTooltipDirty;
		CAProductionTooltipLogic commanderTooltipLogic;
		Widget commanderTooltipWidget;
		bool commanderTooltipBeforeRenderHooked;

		Action commanderTooltipPreviousBeforeRender = () => { };
		Action commanderTooltipBeforeRenderHandler;

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "COMMANDER_PRODUCTION_TOOLTIP";

		public int IconWidth = 62;
		public int IconHeight = 46;
		public int2 IconSpriteOffset = int2.Zero;
		public int HorizontalSpacing = 200;
		public int VerticalSpacing = 110;
		public int ContentPadding = 40;
		public int BorderPadding = 4;
		public float ArrowWidth = 3f;
		public float ArrowHeadLength = 16f;
		public float ArrowHeadWidth = 9f;
		public float BorderWidth = 2f;
		public Color OwnedBorderColor = Color.FromArgb(255, 255, 215, 0);
		public Color HoverBorderColor = Color.FromArgb(160, 220, 255, 255);
		public Color BackgroundColor = Color.FromArgb(160, 20, 20, 20);
		public Color EdgeColor = Color.FromArgb(96, 255, 255, 255);
		public int GroupPadding = 12;
		public float GroupBorderWidth = 2f;
		public Color GroupBorderColor = Color.FromArgb(192, 180, 180, 180);

		public Func<ProductionIcon> GetTooltipIcon;

		[ObjectCreator.UseCtor]
		public CommanderTreeWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			GetTooltipIcon = () => tooltipIcon;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected CommanderTreeWidget(CommanderTreeWidget other)
			: base(other)
		{
			world = other.world;
			worldRenderer = other.worldRenderer;
			TooltipContainer = other.TooltipContainer;
			TooltipTemplate = other.TooltipTemplate;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpriteOffset = other.IconSpriteOffset;
			HorizontalSpacing = other.HorizontalSpacing;
			VerticalSpacing = other.VerticalSpacing;
			ContentPadding = other.ContentPadding;
			BorderPadding = other.BorderPadding;
			ArrowWidth = other.ArrowWidth;
			ArrowHeadLength = other.ArrowHeadLength;
			ArrowHeadWidth = other.ArrowHeadWidth;
			BorderWidth = other.BorderWidth;
			OwnedBorderColor = other.OwnedBorderColor;
			HoverBorderColor = other.HoverBorderColor;
			BackgroundColor = other.BackgroundColor;
			EdgeColor = other.EdgeColor;

			GetTooltipIcon = () => tooltipIcon;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			iconOffset = other.iconOffset;

			if (other.clock != null)
				clock = new Animation(world, clockAnimation);

			if (other.cantBuild != null)
			{
				cantBuild = new Animation(world, notBuildableAnimation);
				cantBuild.PlayFetchIndex(notBuildableSequence, () => 0);
			}
		}

		public override Widget Clone() => new CommanderTreeWidget(this);

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			clock = new Animation(world, clockAnimation);
			cantBuild = new Animation(world, notBuildableAnimation);
			cantBuild.PlayFetchIndex(notBuildableSequence, () => 0);

			iconOffset = 0.5f * new float2(IconWidth, IconHeight) + IconSpriteOffset.ToFloat2();

			overlayFont = Game.Renderer.Fonts["TinyBold"];
			Game.Renderer.Fonts.TryGetValue("Symbols", out symbolFont);
			readyText = FluentProvider.GetMessage("productionpalette-sidebar-production-palette.ready");
			if (string.IsNullOrEmpty(readyText))
				readyText = "READY";
			holdText = FluentProvider.GetMessage("productionpalette-sidebar-production-palette.hold");
			if (string.IsNullOrEmpty(holdText))
				holdText = "ON HOLD";
			readyOffset = iconOffset - overlayFont.Measure(readyText).ToFloat2() / 2f;
			holdOffset = iconOffset - overlayFont.Measure(holdText).ToFloat2() / 2f;
			infiniteOffset = queuedOffset;

			layoutDirty = true;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			currentTooltipToken = tooltipContainer.Value.SetTooltip(
				TooltipTemplate,
				new WidgetArgs { { "player", world.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon }, { "world", world } });

			commanderTooltipWidget = null;
			commanderTooltipLogic = null;
			MarkCommanderTooltipDirty();

			if (EnsureQueue())
				UpdateHover(Viewport.LastMousePos);
		}

		public override void MouseExited()
		{
			ClearCommanderTooltip();
			tooltipIcon = null;

			hoverNode = null;
			lastTooltipNode = null;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (!EnsureQueue())
				return false;

			if (mi.Event == MouseInputEvent.Move)
				UpdateHover(mi.Location);

			if (mi.Event == MouseInputEvent.Scroll)
				return false;

			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right && mi.Button != MouseButton.Middle)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;

			if (!HasMouseFocus)
				return false;

			if (mi.Event == MouseInputEvent.Up)
				return YieldMouseFocus(mi);

			var node = hoverNode;
			if (node == null)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickDisabledSound, null);
				return true;
			}

			switch (mi.Button)
			{
				case MouseButton.Left:
					HandleLeftClick(node, mi.Modifiers);
					break;
				case MouseButton.Right:
					HandleRightClick(node, mi.Modifiers);
					break;
				case MouseButton.Middle:
					HandleMiddleClick(node, mi.Modifiers);
					break;
			}

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down || hoverNode == null)
				return false;

			if (e.Key == Keycode.RETURN || e.Key == Keycode.KP_ENTER || e.Key == Keycode.SPACE)
			{
				HandleLeftClick(hoverNode, e.Modifiers);
				return true;
			}

			return false;
		}

		public override void Tick()
		{
			if (!EnsureQueue())
				return;

			EnsureCommanderTooltipInitialized();
			UpdateNodes();
		}

		void HandleLeftClick(CommanderNode node, Modifiers modifiers)
		{
			if (!node.Available)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickDisabledSound, null);
				return;
			}

			var canQueue = promotionsQueue.CanQueue(node.Actor, out var notification, out var textNotification);
			if (!canQueue)
			{
				Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Speech", notification, world.LocalPlayer?.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(world.LocalPlayer, textNotification);
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickDisabledSound, null);
				return;
			}

			Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickSound, null);
			Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Speech", notification, world.LocalPlayer?.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(world.LocalPlayer, textNotification);

			var handleCount = modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;
			var queued = !modifiers.HasModifier(Modifiers.Ctrl);
			world.IssueOrder(Order.StartProduction(promotionsQueue.Actor, node.Actor.Name, handleCount, queued));
		}

		void HandleRightClick(CommanderNode node, Modifiers modifiers)
		{
			if (node.Icon.Queued.Count == 0)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickDisabledSound, null);
				return;
			}

			Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickSound, null);
			var cancelCount = modifiers.HasModifier(Modifiers.Ctrl)
				? node.Icon.Queued.Count
				: modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;

			world.IssueOrder(Order.CancelProduction(promotionsQueue.Actor, node.Actor.Name, cancelCount));
		}

		void HandleMiddleClick(CommanderNode node, Modifiers modifiers)
		{
			if (node.Icon.Queued.Count == 0)
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickDisabledSound, null);
				return;
			}

			Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickSound, null);
			var cancelCount = modifiers.HasModifier(Modifiers.Ctrl)
				? node.Icon.Queued.Count
				: modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;

			world.IssueOrder(Order.CancelProduction(promotionsQueue.Actor, node.Actor.Name, cancelCount));
		}

		bool EnsureQueue()
		{
			var player = world.LocalPlayer;
			if (player == null)
			{
				promotionsQueue = null;
				nodes.Clear();
				edges.Clear();
				layoutDirty = true;
				return false;
			}

			if (promotionsQueue != null && promotionsQueue.Actor.Owner == player)
			{
				UpdateIconOverlayCache();
				return true;
			}

			promotionsQueue = player.PlayerActor.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => string.Equals(q.Info.Type, PromotionQueueType, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(q.Info.Group, PromotionGroup, StringComparison.OrdinalIgnoreCase));

			nodes.Clear();
			edges.Clear();
			layoutDirty = true;
			UpdateIconOverlayCache();

			return promotionsQueue != null;
		}

		void UpdateNodes()
		{
			visibleActors.Clear();
			sharedQueuedItems.Clear();

			if (promotionsQueue == null)
				return;

			UpdateIconOverlayCache();

			sharedQueuedItems.AddRange(promotionsQueue.AllQueued());

			foreach (var kv in promotionsQueue.Producible)
			{
				var actor = kv.Key;
				var state = kv.Value;

				if (!state.Visible)
					continue;

				if (!actor.Name.StartsWith("promotion.", StringComparison.OrdinalIgnoreCase))
					continue;

				visibleActors.Add(actor);

				if (!nodes.TryGetValue(actor, out var node))
				{
					node = CreateNode(actor);
					nodes[actor] = node;
					layoutDirty = true;
				}

				UpdateNodeState(node, state);
			}

			var removed = nodes.Keys.Where(a => !visibleActors.Contains(a)).ToList();
			if (removed.Count > 0)
			{
				foreach (var actor in removed)
					nodes.Remove(actor);

				layoutDirty = true;
			}

			if (layoutDirty)
				RebuildEdgesAndLayout();
		}

		CommanderNode CreateNode(ActorInfo actor)
		{
			var buildable = BuildableInfo.GetTraitForQueue(actor, promotionsQueue.Info.Type);
			var renderInfo = actor.TraitInfos<RenderSpritesInfo>().FirstOrDefault();
			if (renderInfo == null)
				throw new InvalidOperationException($"Actor {actor.Name} is missing RenderSpritesInfo required for commander tree icons.");

			var animation = new Animation(world, renderInfo.GetImage(actor, promotionsQueue.Actor.Owner.Faction.InternalName));
			animation.Play(buildable.Icon);

			var paletteName = buildable.IconPaletteIsPlayerPalette
				? buildable.IconPalette + promotionsQueue.Actor.Owner.InternalName
				: buildable.IconPalette;

			var icon = new ProductionIcon
			{
				Actor = actor,
				Name = actor.Name,
				Palette = worldRenderer.Palette(paletteName),
				IconClockPalette = worldRenderer.Palette(clockPalette),
				IconDarkenPalette = worldRenderer.Palette(notBuildablePalette),
				Sprite = animation.Image,
				Queued = new List<ProductionItem>(),
				ProductionQueue = promotionsQueue
			};

			var palettePosition = actor.TraitInfos<PromotionPaletteInfo>().FirstOrDefault();
			var node = new CommanderNode(actor, buildable, icon)
			{
				PaletteColumn = palettePosition?.Column ?? -1,
				GroupKey = string.IsNullOrEmpty(palettePosition?.Group) ? null : palettePosition.Group
			};

			return node;
		}

		void UpdateNodeState(CommanderNode node, ProductionState state)
		{
			node.Icon.Queued.Clear();
			node.Icon.Queued.AddRange(sharedQueuedItems.Where(i => i.Item == node.Actor.Name));

			node.Available = promotionsQueue.BuildableItems().Any(a => a == node.Actor);
			node.Buildable = state.Buildable;
			node.Owned = promotionsQueue.TechTree.HasPrerequisites(new[] { node.Actor.Name });
			node.Revealed = state.Visible;
			var wasVisible = node.IsVisible;
			node.IsVisible = state.Visible;
			if (wasVisible != node.IsVisible)
				layoutDirty = true;
		}

		void RebuildEdgesAndLayout()
		{
			edges.Clear();

			var nodeLookup = nodes.Values.ToDictionary(n => n.Actor.Name, n => n, StringComparer.OrdinalIgnoreCase);

			var prerequisiteProviders = new Dictionary<string, List<CommanderNode>>(StringComparer.OrdinalIgnoreCase);
			foreach (var node in nodes.Values)
			{
				foreach (var provider in node.Actor.TraitInfos<ProvidesPrerequisiteInfo>())
				{
					var key = provider.Prerequisite ?? node.Actor.Name;
					if (!prerequisiteProviders.TryGetValue(key, out var list))
					{
						list = new List<CommanderNode>();
						prerequisiteProviders[key] = list;
					}

					if (!list.Contains(node))
						list.Add(node);
				}
			}

			var edgePairs = new HashSet<(CommanderNode from, CommanderNode to)>();
			void AddEdge(CommanderNode from, CommanderNode to)
			{
				if (from == null || to == null || from == to)
					return;

				if (edgePairs.Add((from, to)))
					edges.Add(new CommanderEdge(from, to));
			}

			foreach (var node in nodes.Values)
			{
				var prereqs = node.BuildableInfo.Prerequisites ?? Array.Empty<string>();
				foreach (var raw in prereqs)
				{
					var token = raw;
					if (token.StartsWith("~", StringComparison.Ordinal))
						token = token[1..];

					if (token.StartsWith("!", StringComparison.Ordinal))
						continue;

						if (!nodeLookup.ContainsKey(token) && !prerequisiteProviders.ContainsKey(token))
							continue;

						if (nodeLookup.TryGetValue(token, out var from))
							AddEdge(from, node);

						if (prerequisiteProviders.TryGetValue(token, out var providers))
						{
							foreach (var provider in providers)
								AddEdge(provider, node);
						}
				}
			}

			var depths = new Dictionary<CommanderNode, int>();
			int GetDepth(CommanderNode node)
			{
				if (depths.TryGetValue(node, out var cached))
					return cached;

				var incoming = edges.Where(e => e.To == node).Select(e => e.From).ToArray();
				var depth = incoming.Length == 0 ? 0 : incoming.Max(n => GetDepth(n) + 1);
				depths[node] = depth;
				return depth;
			}

			foreach (var node in nodes.Values)
				GetDepth(node);

			const int MaxLeafColumns = 3;
			var outgoing = nodes.Values.ToDictionary(n => n, _ => new List<CommanderNode>());
			var incomingCounts = nodes.Values.ToDictionary(n => n, _ => 0);

			foreach (var edge in edges)
			{
				outgoing[edge.From].Add(edge.To);
				incomingCounts[edge.To] = incomingCounts.TryGetValue(edge.To, out var count) ? count + 1 : 1;
			}

			int CompareNodes(CommanderNode a, CommanderNode b)
			{
				var order = a.BuildableInfo.BuildPaletteOrder.CompareTo(b.BuildableInfo.BuildPaletteOrder);
				if (order != 0)
					return order;

				return string.Compare(a.Actor.Name, b.Actor.Name, StringComparison.OrdinalIgnoreCase);
			}

			foreach (var list in outgoing.Values)
				list.Sort(CompareNodes);

			var roots = nodes.Values
				.Where(n => !incomingCounts.TryGetValue(n, out var count) || count == 0)
				.OrderBy(n => depths[n])
				.ThenBy(n => n.BuildableInfo.BuildPaletteOrder)
				.ThenBy(n => n.Actor.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (roots.Count == 0)
				roots = nodes.Values
					.OrderBy(n => depths[n])
					.ThenBy(n => n.BuildableInfo.BuildPaletteOrder)
					.ThenBy(n => n.Actor.Name, StringComparer.OrdinalIgnoreCase)
					.ToList();

			var rowRanges = new Dictionary<CommanderNode, (int top, int bottom)>();
			var assigning = new HashSet<CommanderNode>();
			var nextRow = 0;

			(int top, int bottom) AssignRange(CommanderNode node)
			{
				if (rowRanges.TryGetValue(node, out var existing))
					return existing;

				if (!assigning.Add(node))
					return (nextRow, nextRow);

				var children = outgoing[node];
				if (children.Count == 0)
				{
					var row = nextRow++;
					node.RowTop = node.RowBottom = row;
					node.LeafColumnSlot = 0;
					node.LeafColumnCount = 1;
					assigning.Remove(node);
					return rowRanges[node] = (row, row);
				}

				var childSpans = new List<(CommanderNode child, int top, int bottom)>();
				var leafBuffer = new List<CommanderNode>();

				void FlushLeaves()
				{
					if (leafBuffer.Count == 0)
						return;

					var index = 0;
					while (index < leafBuffer.Count)
					{
						var groupSize = Math.Min(MaxLeafColumns, leafBuffer.Count - index);
						var row = nextRow++;
						for (var slot = 0; slot < groupSize; ++slot)
						{
							var leaf = leafBuffer[index + slot];
							leaf.RowTop = leaf.RowBottom = row;
							leaf.LeafColumnSlot = slot;
							leaf.LeafColumnCount = groupSize;
							rowRanges[leaf] = (row, row);
							childSpans.Add((leaf, row, row));
						}
						index += groupSize;
					}

					leafBuffer.Clear();
				}

				foreach (var child in children)
				{
					if (outgoing[child].Count == 0)
					{
						leafBuffer.Add(child);
						continue;
					}

					FlushLeaves();
					var span = AssignRange(child);
					childSpans.Add((child, span.top, span.bottom));
				}

				FlushLeaves();

				if (childSpans.Count == 0)
				{
					var row = nextRow++;
					node.RowTop = node.RowBottom = row;
					node.LeafColumnSlot = 0;
					node.LeafColumnCount = 1;
					assigning.Remove(node);
					return rowRanges[node] = (row, row);
				}

				var top = childSpans.Min(c => c.top);
				var bottom = childSpans.Max(c => c.bottom);
				node.RowTop = top;
				node.RowBottom = bottom;
				node.LeafColumnSlot = 0;
				node.LeafColumnCount = 1;
				assigning.Remove(node);
				return rowRanges[node] = (top, bottom);
			}

			foreach (var root in roots)
				AssignRange(root);

			foreach (var node in nodes.Values)
				AssignRange(node);

			var visibleNodes = nodes.Values.Where(n => n.IsVisible).ToList();
			var orderedRows = visibleNodes
				.GroupBy(n => n.BuildableInfo?.BuildPaletteOrder ?? int.MaxValue)
				.OrderBy(g => g.Key)
				.ToList();

			var remappedRowRanges = new Dictionary<CommanderNode, (int top, int bottom)>();
			var rowColumns = new Dictionary<CommanderNode, (int slot, int count)>();
			var buildOrderRowIndex = 0;
			foreach (var group in orderedRows)
			{
				var orderedRowNodes = group
					.OrderBy(n => depths[n])
					.ThenBy(n => n.Actor.Name, StringComparer.OrdinalIgnoreCase)
					.ToList();

				var assignedSlots = new Dictionary<CommanderNode, int>();
				var usedSlots = new HashSet<int>();

				foreach (var node in orderedRowNodes.Where(n => n.PaletteColumn >= 0).OrderBy(n => n.PaletteColumn))
				{
					var desired = Math.Max(0, node.PaletteColumn);
					var slot = desired;
					while (usedSlots.Contains(slot))
						slot++;

					assignedSlots[node] = slot;
					usedSlots.Add(slot);
				}

				var autoSlot = 0;
				foreach (var node in orderedRowNodes.Where(n => n.PaletteColumn < 0))
				{
					while (usedSlots.Contains(autoSlot))
						autoSlot++;

					assignedSlots[node] = autoSlot;
					usedSlots.Add(autoSlot);
				}

				var slotCount = usedSlots.Count == 0 ? orderedRowNodes.Count : usedSlots.Max() + 1;

				foreach (var node in orderedRowNodes)
				{
					var slot = assignedSlots[node];
					node.RowTop = node.RowBottom = buildOrderRowIndex;
					node.LeafColumnSlot = slot;
					node.LeafColumnCount = slotCount;
					rowColumns[node] = (slot, slotCount);
					remappedRowRanges[node] = (buildOrderRowIndex, buildOrderRowIndex);
				}

				buildOrderRowIndex++;
			}

			rowRanges = remappedRowRanges;

			var rowSpacing = Math.Min(Math.Max(0, VerticalSpacing), IconHeight);
			var rowStep = IconHeight + rowSpacing;
			var leafColumnSpacing = IconWidth + Math.Min(HorizontalSpacing, IconWidth);

			float maxRight = 0f;
			float maxBottom = 0f;

			foreach (var node in nodes.Values)
			{
				var range = rowRanges[node];
				var columnInfo = rowColumns.TryGetValue(node, out var info) ? info : (0, 1);
				var columnSlot = columnInfo.Item1;
				var x = ContentPadding + columnSlot * leafColumnSpacing;

				var centerRow = (range.top + range.bottom) / 2f;
				var y = ContentPadding + centerRow * rowStep;

				node.Position = new float2(x, y);
				node.Bounds = new Rectangle((int)Math.Round((double)x, MidpointRounding.AwayFromZero), (int)Math.Round((double)y, MidpointRounding.AwayFromZero), IconWidth, IconHeight);
				node.Center = node.Position + new float2(IconWidth / 2f, IconHeight / 2f);
				node.TopAnchor = node.Position + new float2(IconWidth / 2f, 0f);
			node.BottomAnchor = node.Position + new float2(IconWidth / 2f, IconHeight);

				maxRight = Math.Max(maxRight, x + IconWidth);
				maxBottom = Math.Max(maxBottom, y + IconHeight);
			}

			UpdateGroups();

			foreach (var group in promotionGroups)
			{
				var groupRight = group.Bounds.X + group.Bounds.Width;
				var groupBottom = group.Bounds.Y + group.Bounds.Height;
				maxRight = Math.Max(maxRight, groupRight);
				maxBottom = Math.Max(maxBottom, groupBottom);
			}

			contentWidth = (int)Math.Ceiling(maxRight + ContentPadding);
			contentHeight = (int)Math.Ceiling(maxBottom + ContentPadding);

			contentWidth = Math.Max(contentWidth, ContentPadding * 2 + IconWidth);
			contentHeight = Math.Max(contentHeight, ContentPadding * 2 + IconHeight);

			var bounds = Bounds;
			bounds.Width = Math.Max(bounds.Width, contentWidth);
			bounds.Height = Math.Max(bounds.Height, contentHeight);
			Bounds = bounds;

			if (Parent is ScrollPanelWidget scrollPanel)
				scrollPanel.ContentHeight = contentHeight;

			layoutDirty = false;
		}

		void UpdateGroups()
		{
			foreach (var node in nodes.Values)
				node.Group = null;

			promotionGroups.Clear();

			if (nodes.Count == 0)
				return;

			var padding = Math.Max(0, GroupPadding);
			var groupedNodes = nodes.Values
				.Where(n => n.IsVisible && !string.IsNullOrEmpty(n.GroupKey))
				.GroupBy(n => n.GroupKey, StringComparer.OrdinalIgnoreCase);

			foreach (var grouping in groupedNodes)
			{
				var members = grouping.ToList();
				if (members.Count == 0)
					continue;

				var left = members.Min(n => n.Position.X);
				var top = members.Min(n => n.Position.Y);
				var right = members.Max(n => n.Position.X + IconWidth);
				var bottom = members.Max(n => n.Position.Y + IconHeight);

				var rect = new Rectangle(
					(int)Math.Floor(left) - padding,
					(int)Math.Floor(top) - padding,
					Math.Max(1, (int)Math.Ceiling(right - left) + padding * 2),
					Math.Max(1, (int)Math.Ceiling(bottom - top) + padding * 2));

				var group = new CommanderGroup(grouping.Key);
				group.Bounds = rect;
				var topLeft = new float2(rect.X, rect.Y);
				var size = new float2(rect.Width, rect.Height);
				var center = topLeft + size / 2f;
				group.Center = center;
				group.TopAnchor = new float2(center.X, rect.Y);
				group.BottomAnchor = new float2(center.X, rect.Y + rect.Height);
				group.Nodes.AddRange(members);

				promotionGroups.Add(group);

				foreach (var member in members)
					member.Group = group;
			}

			promotionGroups.Sort((a, b) =>
			{
				var order = a.Bounds.Y.CompareTo(b.Bounds.Y);
				if (order != 0)
					return order;

				order = a.Bounds.X.CompareTo(b.Bounds.X);
				if (order != 0)
					return order;

				return string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
			});
		}

		void UpdateHover(int2 mouseLocation)
		{
			var local = mouseLocation - RenderOrigin;
			hoverNode = nodes.Values.FirstOrDefault(n => n.Bounds.Contains(local));
			tooltipIcon = hoverNode?.Icon;

			if (TooltipContainer != null && hoverNode != lastTooltipNode)
			{
				if (hoverNode == null)
				{
					ClearCommanderTooltip();
				}
				else
				{
					currentTooltipToken = tooltipContainer.Value.SetTooltip(
						TooltipTemplate,
						new WidgetArgs { { "player", world.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon }, { "world", world } });

					commanderTooltipWidget = null;
					commanderTooltipLogic = null;
					MarkCommanderTooltipDirty();
				}

				lastTooltipNode = hoverNode;
			}
		}


		void MarkCommanderTooltipDirty()

		{

			commanderTooltipDirty = true;



			if (TooltipContainer == null || !tooltipContainer.IsValueCreated)

			{

				return;

			}



			var container = tooltipContainer.Value;

			container.TooltipDelayMilliseconds = 0;



			if (commanderTooltipBeforeRenderHooked)

			{

				return;

			}



			commanderTooltipBeforeRenderHooked = true;

			commanderTooltipPreviousBeforeRender = container.BeforeRender ?? (() => { });

			commanderTooltipBeforeRenderHandler = () =>

			{

				if (commanderTooltipDirty)

				{

					EnsureCommanderTooltipInitialized();

				}



				commanderTooltipPreviousBeforeRender();

			};

			container.BeforeRender = commanderTooltipBeforeRenderHandler;

		}








		void EnsureCommanderTooltipInitialized()

		{

			if (TooltipContainer == null || !tooltipContainer.IsValueCreated)

				return;



			var container = tooltipContainer.Value;

			if (commanderTooltipWidget != null && !container.Children.Any(c => c == commanderTooltipWidget))

			{

				DisposeCommanderTooltipLogic();

				commanderTooltipWidget = null;

				commanderTooltipDirty = commanderTooltipDirty || hoverNode != null;

			}



			if (!commanderTooltipDirty || hoverNode == null)

				return;



			var tooltipWidget = container.Children.LastOrDefault();

			if (tooltipWidget == null)

			{

				return;

			}



			if (tooltipWidget != commanderTooltipWidget)

			{

				DisposeCommanderTooltipLogic();

				commanderTooltipWidget = tooltipWidget;

				commanderTooltipLogic = tooltipWidget.LogicObjects?.OfType<CAProductionTooltipLogic>().FirstOrDefault();

			}



			if (commanderTooltipLogic == null)

			{

				var player = world.LocalPlayer;

				if (player == null)

					return;



				commanderTooltipLogic = new CAProductionTooltipLogic(tooltipWidget, container, player, GetTooltipIcon);

				Ui.Subscribe(commanderTooltipLogic);

				commanderTooltipPreviousBeforeRender = container.BeforeRender;

			}



			commanderTooltipDirty = commanderTooltipLogic == null;

			if (!commanderTooltipDirty && commanderTooltipBeforeRenderHooked)

			{

				container.BeforeRender = commanderTooltipPreviousBeforeRender;

				commanderTooltipBeforeRenderHooked = false;

				commanderTooltipBeforeRenderHandler = null;

			}

		}








		void DisposeCommanderTooltipLogic()

		{

			if (commanderTooltipLogic == null)

				return;

			Ui.Unsubscribe(commanderTooltipLogic);

			commanderTooltipLogic.Dispose();

			commanderTooltipLogic = null;

		}






		void ClearCommanderTooltip()

		{

			if (TooltipContainer != null && tooltipContainer.IsValueCreated)

			{

				var container = tooltipContainer.Value;



				if (currentTooltipToken != 0)

					container.RemoveTooltip(currentTooltipToken);

				else

					container.RemoveTooltip();



				if (commanderTooltipBeforeRenderHooked)

				{

					container.BeforeRender = commanderTooltipPreviousBeforeRender;

					commanderTooltipBeforeRenderHooked = false;

					commanderTooltipBeforeRenderHandler = null;

				}

			}



			DisposeCommanderTooltipLogic();

			commanderTooltipDirty = false;

			commanderTooltipWidget = null;

			currentTooltipToken = 0;

			commanderTooltipPreviousBeforeRender = () => { };

		}







		public override void Draw()
		{
			if (BackgroundColor.A > 0)
				WidgetUtils.FillRectWithColor(RenderBounds, BackgroundColor);

			if (!EnsureQueue() || nodes.Count == 0)
				return;

			Game.Renderer.EnableAntialiasingFilter();
			DrawGroups();
			DrawEdges();
			Game.Renderer.DisableAntialiasingFilter();

			Game.Renderer.EnableAntialiasingFilter();
			foreach (var node in nodes.Values)
				DrawNode(node);
			Game.Renderer.DisableAntialiasingFilter();
		}

		void DrawGroups()
		{
			if (promotionGroups.Count == 0)
				return;

			if (GroupBorderColor.A == 0 || GroupBorderWidth <= 0f)
				return;

			foreach (var group in promotionGroups)
			{
				var topLeft = RenderOrigin.ToFloat2() + new float2(group.Bounds.X, group.Bounds.Y);
				var bottomRight = topLeft + new float2(group.Bounds.Width, group.Bounds.Height);
				Game.Renderer.RgbaColorRenderer.DrawRect(new float3(topLeft, 0f), new float3(bottomRight, 0f), GroupBorderWidth, GroupBorderColor);
			}
		}

		void DrawEdges()
		{
			if (edges.Count == 0)
				return;

			var renderedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var edge in edges)
			{
				var from = edge.From;
				var to = edge.To;
				if (!from.IsVisible || !to.IsVisible)
					continue;

				var fromKey = GetEdgeContainerKey(from);
				var toKey = GetEdgeContainerKey(to);
				var dedupKey = fromKey + "->" + toKey;
				if (!renderedKeys.Add(dedupKey))
					continue;

				var edgeColor = (!from.Revealed || !to.Revealed) ? Color.FromArgb(EdgeColor.A / 2, EdgeColor.R, EdgeColor.G, EdgeColor.B) : EdgeColor;
				var start = RenderOrigin.ToFloat2() + GetEdgeStartAnchor(from);
				var end = RenderOrigin.ToFloat2() + GetEdgeEndAnchor(to);
				var dir = end - start;
				if (dir.LengthSquared < 1f)
					continue;

				var norm = dir / dir.Length;
				var arrowStart = end - norm * ArrowHeadLength;
				var perp = new float2(-norm.Y, norm.X);
				var left = arrowStart + perp * ArrowHeadWidth;
				var right = arrowStart - perp * ArrowHeadWidth;

				Game.Renderer.RgbaColorRenderer.DrawLine(new float3(start, 0f), new float3(arrowStart, 0f), ArrowWidth, edgeColor);
				Game.Renderer.RgbaColorRenderer.DrawLine(new float3(left, 0f), new float3(end, 0f), ArrowWidth, edgeColor);
				Game.Renderer.RgbaColorRenderer.DrawLine(new float3(right, 0f), new float3(end, 0f), ArrowWidth, edgeColor);
			}
		}

		string GetEdgeContainerKey(CommanderNode node)
		{
			return node.Group != null ? $"group:{node.Group.Key}" : $"node:{node.Actor.Name}";
		}

		float2 GetEdgeStartAnchor(CommanderNode node)
		{
			return node.Group != null ? node.Group.BottomAnchor : node.BottomAnchor;
		}

		float2 GetEdgeEndAnchor(CommanderNode node)
		{
			return node.Group != null ? node.Group.TopAnchor : node.TopAnchor;
		}

		void DrawNode(CommanderNode node)
		{
			if (!node.IsVisible)
				return;

			var topLeft = RenderOrigin.ToFloat2() + node.Position;
			var center = topLeft + iconOffset;

			WidgetUtils.DrawSpriteCentered(node.Icon.Sprite, node.Icon.Palette, center);

			var iconSize = new float2(IconWidth, IconHeight);
			if (iconOverlays.Length > 0)
			{
				foreach (var overlay in iconOverlays)
				{
					if (!overlay.IsOverlayActive(node.Actor, promotionsQueue.Actor))
						continue;

					WidgetUtils.DrawSpriteCentered(overlay.Sprite, worldRenderer.Palette(overlay.Palette), center + overlay.Offset(iconSize));
				}
			}

			if (node.Icon.Queued.Count > 0)
			{
				var first = node.Icon.Queued[0];
				clock.PlayFetchIndex(clockSequence,
					() => (first.TotalTime - first.RemainingTime) * (clock.CurrentSequence.Length - 1) / Math.Max(1, first.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSpriteCentered(clock.Image, node.Icon.IconClockPalette, center);
			}
			else if (!node.Available && !node.Owned)
			{
				WidgetUtils.DrawSpriteCentered(cantBuild.Image, node.Icon.IconDarkenPalette, center);
			}

			var iconPos = RenderOrigin.ToFloat2() + node.Position;
			var totalQueued = node.Icon.Queued.Count;
			if (totalQueued > 0)
			{
				var first = node.Icon.Queued[0];
				var waiting = !promotionsQueue.IsProducing(first) && !first.Done;

				if (first.Done)
					overlayFont.DrawTextWithContrast(readyText, iconPos + readyOffset, overlayTextColor, Color.Black, 1);
				else if (first.Paused)
					overlayFont.DrawTextWithContrast(holdText, iconPos + holdOffset, overlayTextColor, Color.Black, 1);
				else if (!waiting)
				{
					var timeText = WidgetUtils.FormatTime(first.Queue.RemainingTimeActual(first), world.Timestep);
					var timeOffset = iconOffset - overlayFont.Measure(timeText).ToFloat2() / 2f;
					overlayFont.DrawTextWithContrast(timeText, iconPos + timeOffset, overlayTextColor, Color.Black, 1);
				}

				if (first.Infinite && symbolFont != null)
					symbolFont.DrawTextWithContrast(infiniteSymbol, iconPos + infiniteOffset, overlayTextColor, Color.Black, 1);
				else if (totalQueued > 1 || waiting)
					overlayFont.DrawTextWithContrast(totalQueued.ToStringInvariant(), iconPos + queuedOffset, overlayTextColor, Color.Black, 1);
			}

			DrawNodeBorder(node, topLeft);
		}

		void UpdateIconOverlayCache()
		{
			if (promotionsQueue == null)
			{
				cachedQueueOwner = null;
				iconOverlays = Array.Empty<IProductionIconOverlay>();
				return;
			}

			var owner = promotionsQueue.Actor.Owner;
			if (cachedQueueOwner == owner)
				return;

			cachedQueueOwner = owner;
			iconOverlays = owner.World
				.ActorsWithTrait<IProductionIconOverlay>()
				.Where(a => a.Actor.Owner == owner)
				.Select(a => a.Trait)
				.ToArray();
		}

		void DrawNodeBorder(CommanderNode node, float2 topLeft)
		{
			var isHover = node == hoverNode;
			var borderColor = node.Owned ? OwnedBorderColor : isHover ? HoverBorderColor : Color.FromArgb(0, 0, 0, 0);
			if (borderColor.A == 0)
				return;

			var padding = BorderPadding;
			var rectTopLeft = topLeft - new float2(padding, padding);
			var rectBottomRight = rectTopLeft + new float2(IconWidth + padding * 2, IconHeight + padding * 2);
			Game.Renderer.RgbaColorRenderer.DrawRect(new float3(rectTopLeft, 0f), new float3(rectBottomRight, 0f), BorderWidth, borderColor);
		}

		public override void Removed()
		{
			ClearCommanderTooltip();
			base.Removed();
		}

		sealed class CommanderNode
		{
			public CommanderNode(ActorInfo actor, BuildableInfo buildableInfo, ProductionIcon icon)
			{
				Actor = actor;
				BuildableInfo = buildableInfo;
				Icon = icon;
			}

			public ActorInfo Actor { get; }
			public BuildableInfo BuildableInfo { get; }
			public ProductionIcon Icon { get; }
			public bool Available { get; set; }
			public bool Buildable { get; set; }
			public bool Owned { get; set; }
			public bool Revealed { get; set; }

			public bool IsVisible { get; set; } = true;
			public float2 Position { get; set; }
			public Rectangle Bounds { get; set; }
			public float2 Center { get; set; }
			public float2 TopAnchor { get; set; }
			public float2 BottomAnchor { get; set; }
			public int RowTop { get; set; }
			public int RowBottom { get; set; }
			public int LeafColumnSlot { get; set; }
			public int LeafColumnCount { get; set; } = 1;
			public int PaletteColumn { get; set; } = -1;
			public string GroupKey { get; set; }
			public CommanderGroup Group { get; set; }
		}

		sealed class CommanderEdge
		{
			public CommanderEdge(CommanderNode from, CommanderNode to)
			{
				From = from;
				To = to;
			}

			public CommanderNode From { get; }
			public CommanderNode To { get; }
		}

		sealed class CommanderGroup
		{
			public CommanderGroup(string key)
			{
				Key = key;
			}

			public string Key { get; }
			public List<CommanderNode> Nodes { get; } = new();
			public Rectangle Bounds { get; set; }
			public float2 Center { get; set; }
			public float2 TopAnchor { get; set; }
			public float2 BottomAnchor { get; set; }
		}
	}
}









