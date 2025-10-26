using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GOFUS.Inventory;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Enhanced Inventory UI with full drag-drop support
    /// Implements Dofus-style inventory management
    /// </summary>
    public class EnhancedInventoryUI : UIScreen, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        #region Properties

        public int MaxSlots { get; private set; }
        public int ItemCount => items.Count(i => i != null);
        public int FreeSlots => MaxSlots - ItemCount;
        public float CurrentWeight { get; private set; }
        public float MaxWeight { get; private set; } = float.MaxValue;
        public float RemainingWeight => MaxWeight - CurrentWeight;

        // Drag & Drop
        public bool IsDragging { get; private set; }
        public InventoryItem DraggedItem { get; private set; }
        public int DragSourceSlot { get; private set; }

        private InventoryItem[] items;
        private Dictionary<EquipmentSlot, InventoryItem> equippedItems;
        private Dictionary<int, InventoryItem> hotbarItems;
        private List<GameObject> slotObjects;
        private GameObject dragIcon;
        private ItemType? currentFilter;
        private SortMode currentSortMode = SortMode.None;

        #endregion

        #region Events

        public event Action<InventoryItem> OnItemAdded;
        public event Action<InventoryItem> OnItemRemoved;
        public event Action<InventoryItem> OnItemUsed;
        public event Action<InventoryItem, EquipmentSlot> OnItemEquipped;
        public event Action<EquipmentSlot> OnItemUnequipped;

        #endregion

        #region UI Elements

        private Transform inventoryGrid;
        private Transform equipmentPanel;
        private TextMeshProUGUI weightText;
        private TMP_InputField searchField;
        private TMP_Dropdown filterDropdown;
        private TMP_Dropdown sortDropdown;
        private GameObject itemTooltip;
        private GameObject contextMenu;

        #endregion

        #region Initialization

        public override void Initialize()
        {
            base.Initialize();
            Initialize(40); // Default 40 slots like Dofus
        }

        public void Initialize(int slots)
        {
            MaxSlots = slots;
            items = new InventoryItem[MaxSlots];
            equippedItems = new Dictionary<EquipmentSlot, InventoryItem>();
            hotbarItems = new Dictionary<int, InventoryItem>();
            slotObjects = new List<GameObject>();

            CreateInventoryUI();
            CreateEquipmentPanel();
            CreateDragIcon();
            SetupEventHandlers();
        }

        private void CreateInventoryUI()
        {
            // Main container
            GameObject container = new GameObject("InventoryContainer");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(400, 500);

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // Title
            CreateTitle(container.transform);

            // Search and filters
            CreateSearchAndFilters(container.transform);

            // Inventory grid
            CreateInventoryGrid(container.transform);

            // Weight display
            CreateWeightDisplay(container.transform);
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(-20, 30);

            TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "Inventory";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }

        private void CreateSearchAndFilters(Transform parent)
        {
            GameObject filterContainer = new GameObject("FilterContainer");
            filterContainer.transform.SetParent(parent, false);

            RectTransform rect = filterContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -50);
            rect.sizeDelta = new Vector2(-20, 30);

            HorizontalLayoutGroup layout = filterContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;

            // Search field
            CreateSearchField(filterContainer.transform);

            // Filter dropdown
            CreateFilterDropdown(filterContainer.transform);

            // Sort dropdown
            CreateSortDropdown(filterContainer.transform);
        }

        private void CreateSearchField(Transform parent)
        {
            GameObject searchObj = new GameObject("SearchField");
            searchObj.transform.SetParent(parent, false);

            searchField = searchObj.AddComponent<TMP_InputField>();
            searchField.placeholder.GetComponent<TextMeshProUGUI>().text = "Search...";

            LayoutElement layout = searchObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 150;
            layout.preferredHeight = 30;
        }

        private void CreateFilterDropdown(Transform parent)
        {
            GameObject dropdownObj = new GameObject("FilterDropdown");
            dropdownObj.transform.SetParent(parent, false);

            filterDropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            filterDropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("All"),
                new TMP_Dropdown.OptionData("Weapons"),
                new TMP_Dropdown.OptionData("Equipment"),
                new TMP_Dropdown.OptionData("Consumables"),
                new TMP_Dropdown.OptionData("Resources"),
                new TMP_Dropdown.OptionData("Quest")
            };

            LayoutElement layout = dropdownObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 100;
            layout.preferredHeight = 30;
        }

        private void CreateSortDropdown(Transform parent)
        {
            GameObject dropdownObj = new GameObject("SortDropdown");
            dropdownObj.transform.SetParent(parent, false);

            sortDropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            sortDropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("None"),
                new TMP_Dropdown.OptionData("Name"),
                new TMP_Dropdown.OptionData("Value"),
                new TMP_Dropdown.OptionData("Quantity"),
                new TMP_Dropdown.OptionData("Type"),
                new TMP_Dropdown.OptionData("Weight")
            };

            LayoutElement layout = dropdownObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 100;
            layout.preferredHeight = 30;
        }

        private void CreateInventoryGrid(Transform parent)
        {
            GameObject gridObj = new GameObject("InventoryGrid");
            gridObj.transform.SetParent(parent, false);

            RectTransform rect = gridObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(-20, -120);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(40, 40);
            grid.spacing = new Vector2(5, 5);
            grid.padding = new RectOffset(10, 10, 10, 10);

            inventoryGrid = gridObj.transform;

            // Create slots
            for (int i = 0; i < MaxSlots; i++)
            {
                CreateInventorySlot(i);
            }
        }

        private void CreateInventorySlot(int index)
        {
            GameObject slot = new GameObject($"Slot_{index}");
            slot.transform.SetParent(inventoryGrid, false);

            Image slotImage = slot.AddComponent<Image>();
            slotImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // Add slot index component
            InventorySlot slotComponent = slot.AddComponent<InventorySlot>();
            slotComponent.SlotIndex = index;
            slotComponent.ParentInventory = this;

            slotObjects.Add(slot);
        }

        private void CreateEquipmentPanel(Transform parent = null)
        {
            GameObject panel = new GameObject("EquipmentPanel");
            panel.transform.SetParent(parent ?? transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(-450, 0);
            rect.sizeDelta = new Vector2(200, 400);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

            equipmentPanel = panel.transform;

            // Create equipment slots
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                CreateEquipmentSlot(slot);
            }
        }

        private void CreateEquipmentSlot(EquipmentSlot slot)
        {
            GameObject slotObj = new GameObject($"EquipSlot_{slot}");
            slotObj.transform.SetParent(equipmentPanel, false);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            Vector2 position = GetEquipmentSlotPosition(slot);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(40, 40);

            Image slotImage = slotObj.AddComponent<Image>();
            slotImage.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

            EquipmentSlotUI slotComponent = slotObj.AddComponent<EquipmentSlotUI>();
            slotComponent.Slot = slot;
            slotComponent.ParentInventory = this;
        }

        private Vector2 GetEquipmentSlotPosition(EquipmentSlot slot)
        {
            // Arrange equipment slots in character-like positions
            switch (slot)
            {
                case EquipmentSlot.Head: return new Vector2(100, 150);
                case EquipmentSlot.Chest: return new Vector2(100, 100);
                case EquipmentSlot.Legs: return new Vector2(100, 50);
                case EquipmentSlot.Feet: return new Vector2(100, 0);
                case EquipmentSlot.MainHand: return new Vector2(50, 100);
                case EquipmentSlot.OffHand: return new Vector2(150, 100);
                case EquipmentSlot.Ring1: return new Vector2(50, 50);
                case EquipmentSlot.Ring2: return new Vector2(150, 50);
                case EquipmentSlot.Amulet: return new Vector2(100, 180);
                default: return Vector2.zero;
            }
        }

        private void CreateWeightDisplay(Transform parent)
        {
            GameObject weightObj = new GameObject("WeightDisplay");
            weightObj.transform.SetParent(parent, false);

            RectTransform rect = weightObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 10);
            rect.sizeDelta = new Vector2(-20, 20);

            weightText = weightObj.AddComponent<TextMeshProUGUI>();
            weightText.text = "Weight: 0 / ∞";
            weightText.fontSize = 14;
            weightText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateDragIcon()
        {
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(transform.parent, false);

            RectTransform rect = dragIcon.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40);

            Image icon = dragIcon.AddComponent<Image>();
            icon.raycastTarget = false;

            dragIcon.SetActive(false);
        }

        #endregion

        #region Item Management

        public bool AddItem(InventoryItem item)
        {
            // Check weight
            if (CurrentWeight + item.Weight > MaxWeight)
                return false;

            // Try to stack if stackable
            if (item.IsStackable)
            {
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (items[i] != null && items[i].Id == item.Id)
                    {
                        int canStack = items[i].MaxStack - items[i].Quantity;
                        if (canStack > 0)
                        {
                            int toAdd = Mathf.Min(canStack, item.Quantity);
                            items[i].Quantity += toAdd;
                            item.Quantity -= toAdd;

                            if (item.Quantity <= 0)
                            {
                                UpdateWeight();
                                OnItemAdded?.Invoke(item);
                                return true;
                            }
                        }
                    }
                }
            }

            // Find first empty slot
            for (int i = 0; i < MaxSlots; i++)
            {
                if (items[i] == null)
                {
                    items[i] = item;
                    UpdateWeight();
                    UpdateSlotVisual(i);
                    OnItemAdded?.Invoke(item);
                    return true;
                }
            }

            return false;
        }

        public bool AddItemToSlot(InventoryItem item, int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return false;
            if (items[slot] != null) return false;

            items[slot] = item;
            UpdateWeight();
            UpdateSlotVisual(slot);
            OnItemAdded?.Invoke(item);
            return true;
        }

        public InventoryItem GetItemAtSlot(int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return null;
            return items[slot];
        }

        public bool RemoveItem(int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return false;
            if (items[slot] == null) return false;

            var item = items[slot];
            items[slot] = null;
            UpdateWeight();
            UpdateSlotVisual(slot);
            OnItemRemoved?.Invoke(item);
            return true;
        }

        public List<InventoryItem> GetAllItems()
        {
            return items.Where(i => i != null).ToList();
        }

        public List<InventoryItem> GetVisibleItems()
        {
            var allItems = GetAllItems();

            if (currentFilter.HasValue)
            {
                allItems = allItems.Where(i => i.Type == currentFilter.Value).ToList();
            }

            if (!string.IsNullOrEmpty(searchField?.text))
            {
                allItems = allItems.Where(i => i.Name.ToLower().Contains(searchField.text.ToLower())).ToList();
            }

            return allItems;
        }

        #endregion

        #region Drag and Drop

        public void StartDrag(int slot)
        {
            if (slot < 0 || slot >= MaxSlots || items[slot] == null) return;

            IsDragging = true;
            DraggedItem = items[slot];
            DragSourceSlot = slot;

            // Visual feedback
            if (dragIcon != null)
            {
                dragIcon.SetActive(true);
                dragIcon.GetComponent<Image>().sprite = GetItemSprite(DraggedItem);
            }
        }

        public void EndDrag()
        {
            IsDragging = false;
            DraggedItem = null;
            DragSourceSlot = -1;

            if (dragIcon != null)
                dragIcon.SetActive(false);
        }

        public bool DropOnSlot(int targetSlot)
        {
            if (!IsDragging || targetSlot < 0 || targetSlot >= MaxSlots) return false;

            var targetItem = items[targetSlot];
            var sourceItem = items[DragSourceSlot];

            // If dropping on same slot, do nothing
            if (targetSlot == DragSourceSlot)
            {
                EndDrag();
                return false;
            }

            // If target is empty, just move
            if (targetItem == null)
            {
                items[targetSlot] = sourceItem;
                items[DragSourceSlot] = null;
            }
            // If same item and stackable, try to merge
            else if (targetItem.Id == sourceItem.Id && targetItem.IsStackable)
            {
                int canStack = targetItem.MaxStack - targetItem.Quantity;
                int toAdd = Mathf.Min(canStack, sourceItem.Quantity);

                targetItem.Quantity += toAdd;
                sourceItem.Quantity -= toAdd;

                if (sourceItem.Quantity <= 0)
                    items[DragSourceSlot] = null;
            }
            // Otherwise, swap items
            else
            {
                items[targetSlot] = sourceItem;
                items[DragSourceSlot] = targetItem;
            }

            UpdateSlotVisual(DragSourceSlot);
            UpdateSlotVisual(targetSlot);
            EndDrag();
            return true;
        }

        public void SplitStack(int slot, int amount)
        {
            if (slot < 0 || slot >= MaxSlots || items[slot] == null) return;
            if (!items[slot].IsStackable || items[slot].Quantity <= amount) return;

            // Find empty slot
            int emptySlot = -1;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (items[i] == null)
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot == -1) return;

            // Create new stack
            var newStack = new InventoryItem
            {
                Id = items[slot].Id,
                Name = items[slot].Name,
                Quantity = amount,
                Type = items[slot].Type,
                IsStackable = true,
                MaxStack = items[slot].MaxStack,
                Weight = items[slot].Weight
            };

            items[slot].Quantity -= amount;
            items[emptySlot] = newStack;

            UpdateSlotVisual(slot);
            UpdateSlotVisual(emptySlot);
        }

        #endregion

        #region Equipment

        public bool EquipItem(int slot)
        {
            if (slot < 0 || slot >= MaxSlots || items[slot] == null) return false;

            var item = items[slot];
            if (item.Type != ItemType.Equipment) return false;

            return EquipToSlot(item, item.EquipmentSlot);
        }

        public bool EquipToSlot(InventoryItem item, EquipmentSlot equipSlot)
        {
            if (item.Type != ItemType.Equipment) return false;

            // Swap if something is already equipped
            if (equippedItems.ContainsKey(equipSlot))
            {
                var currentEquipped = equippedItems[equipSlot];
                if (!AddItem(currentEquipped))
                    return false;
            }

            equippedItems[equipSlot] = item;

            // Remove from inventory
            for (int i = 0; i < MaxSlots; i++)
            {
                if (items[i] == item)
                {
                    items[i] = null;
                    UpdateSlotVisual(i);
                    break;
                }
            }

            OnItemEquipped?.Invoke(item, equipSlot);
            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.ContainsKey(slot)) return false;

            var item = equippedItems[slot];
            if (!AddItem(item)) return false;

            equippedItems.Remove(slot);
            OnItemUnequipped?.Invoke(slot);
            return true;
        }

        public InventoryItem GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
        }

        #endregion

        #region Filtering and Sorting

        public void SetFilter(ItemType? type)
        {
            currentFilter = type;
            RefreshDisplay();
        }

        public void SortBy(SortMode mode)
        {
            currentSortMode = mode;

            var itemsList = items.Where(i => i != null).ToList();

            switch (mode)
            {
                case SortMode.Name:
                    itemsList = itemsList.OrderBy(i => i.Name).ToList();
                    break;
                case SortMode.Value:
                    itemsList = itemsList.OrderByDescending(i => i.Value).ToList();
                    break;
                case SortMode.Quantity:
                    itemsList = itemsList.OrderByDescending(i => i.Quantity).ToList();
                    break;
                case SortMode.Type:
                    itemsList = itemsList.OrderBy(i => i.Type).ToList();
                    break;
                case SortMode.Weight:
                    itemsList = itemsList.OrderByDescending(i => i.Weight).ToList();
                    break;
            }

            // Clear and refill array
            Array.Clear(items, 0, items.Length);
            int index = 0;
            foreach (var item in itemsList)
            {
                items[index++] = item;
            }

            RefreshDisplay();
        }

        #endregion

        #region Quick Actions

        public bool UseItem(int slot)
        {
            if (slot < 0 || slot >= MaxSlots || items[slot] == null) return false;

            var item = items[slot];
            if (item.Type != ItemType.Consumable) return false;

            item.Quantity--;
            if (item.Quantity <= 0)
                items[slot] = null;

            UpdateSlotVisual(slot);
            OnItemUsed?.Invoke(item);
            return true;
        }

        public bool DestroyItem(int slot)
        {
            return RemoveItem(slot);
        }

        public InventoryItem DropItem(int slot)
        {
            if (slot < 0 || slot >= MaxSlots || items[slot] == null) return null;

            var item = items[slot];
            RemoveItem(slot);
            return item;
        }

        public List<string> GetContextMenuOptions(int slot)
        {
            var options = new List<string>();
            if (slot < 0 || slot >= MaxSlots || items[slot] == null)
                return options;

            var item = items[slot];

            if (item.Type == ItemType.Consumable)
                options.Add("Use");

            if (item.Type == ItemType.Equipment || item.Type == ItemType.Weapon)
                options.Add("Equip");

            options.Add("Drop");
            options.Add("Destroy");

            if (item.IsStackable && item.Quantity > 1)
                options.Add("Split");

            return options;
        }

        #endregion

        #region Weight System

        public void SetMaxWeight(float weight)
        {
            MaxWeight = weight;
            UpdateWeightDisplay();
        }

        private void UpdateWeight()
        {
            CurrentWeight = 0;
            foreach (var item in items)
            {
                if (item != null)
                    CurrentWeight += item.Weight * item.Quantity;
            }

            UpdateWeightDisplay();
        }

        private void UpdateWeightDisplay()
        {
            if (weightText != null)
            {
                string maxWeightStr = MaxWeight == float.MaxValue ? "∞" : MaxWeight.ToString("F1");
                weightText.text = $"Weight: {CurrentWeight:F1} / {maxWeightStr}";

                if (CurrentWeight > MaxWeight * 0.9f)
                    weightText.color = Color.red;
                else if (CurrentWeight > MaxWeight * 0.7f)
                    weightText.color = Color.yellow;
                else
                    weightText.color = Color.white;
            }
        }

        #endregion

        #region Search

        public List<InventoryItem> SearchItems(string query)
        {
            if (string.IsNullOrEmpty(query)) return GetAllItems();

            return items.Where(i => i != null &&
                i.Name.ToLower().Contains(query.ToLower())).ToList();
        }

        #endregion

        #region Hotbar

        public void AssignToHotbar(int inventorySlot, int hotbarSlot)
        {
            if (inventorySlot < 0 || inventorySlot >= MaxSlots || items[inventorySlot] == null)
                return;

            hotbarItems[hotbarSlot] = items[inventorySlot];
        }

        public InventoryItem GetHotbarItem(int hotbarSlot)
        {
            return hotbarItems.ContainsKey(hotbarSlot) ? hotbarItems[hotbarSlot] : null;
        }

        public bool ActivateHotbar(int hotbarSlot)
        {
            if (!hotbarItems.ContainsKey(hotbarSlot)) return false;

            var item = hotbarItems[hotbarSlot];

            // Find item in inventory
            for (int i = 0; i < MaxSlots; i++)
            {
                if (items[i] == item)
                {
                    if (item.Type == ItemType.Consumable)
                        return UseItem(i);
                    break;
                }
            }

            return false;
        }

        #endregion

        #region UI Updates

        private void UpdateSlotVisual(int slot)
        {
            if (slot < 0 || slot >= slotObjects.Count) return;

            var slotObj = slotObjects[slot];
            var item = items[slot];

            // Update icon
            Transform iconTransform = slotObj.transform.Find("Icon");
            if (item != null)
            {
                if (iconTransform == null)
                {
                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(slotObj.transform, false);
                    iconTransform = iconObj.transform;

                    RectTransform rect = iconObj.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;

                    Image icon = iconObj.AddComponent<Image>();
                    icon.sprite = GetItemSprite(item);
                }

                // Update quantity text
                UpdateQuantityText(slotObj, item);
            }
            else if (iconTransform != null)
            {
                Destroy(iconTransform.gameObject);
            }
        }

        private void UpdateQuantityText(GameObject slot, InventoryItem item)
        {
            if (item.Quantity <= 1) return;

            Transform textTransform = slot.transform.Find("Quantity");
            if (textTransform == null)
            {
                GameObject textObj = new GameObject("Quantity");
                textObj.transform.SetParent(slot.transform, false);

                RectTransform rect = textObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(-2, 2);

                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = item.Quantity.ToString();
                text.fontSize = 10;
                text.alignment = TextAlignmentOptions.BottomRight;
            }
            else
            {
                textTransform.GetComponent<TextMeshProUGUI>().text = item.Quantity.ToString();
            }
        }

        private void RefreshDisplay()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                UpdateSlotVisual(i);
            }
        }

        private Sprite GetItemSprite(InventoryItem item)
        {
            // Placeholder - would load actual sprite
            return null;
        }

        private void SetupEventHandlers()
        {
            // Setup search field
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener((text) => RefreshDisplay());
            }

            // Setup filter dropdown
            if (filterDropdown != null)
            {
                filterDropdown.onValueChanged.AddListener((index) =>
                {
                    if (index == 0) SetFilter(null);
                    else SetFilter((ItemType)(index - 1));
                });
            }

            // Setup sort dropdown
            if (sortDropdown != null)
            {
                sortDropdown.onValueChanged.AddListener((index) =>
                {
                    SortBy((SortMode)index);
                });
            }
        }

        #endregion

        #region Drag Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Show context menu
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Handled by InventorySlot component
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIcon != null && IsDragging)
            {
                dragIcon.transform.position = Input.mousePosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handled by InventorySlot component
        }

        #endregion
    }

    #region Supporting Classes

    public class InventorySlot : MonoBehaviour, IDropHandler, IBeginDragHandler
    {
        public int SlotIndex { get; set; }
        public EnhancedInventoryUI ParentInventory { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ParentInventory?.StartDrag(SlotIndex);
        }

        public void OnDrop(PointerEventData eventData)
        {
            ParentInventory?.DropOnSlot(SlotIndex);
        }
    }

    public class EquipmentSlotUI : MonoBehaviour, IDropHandler
    {
        public EquipmentSlot Slot { get; set; }
        public EnhancedInventoryUI ParentInventory { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            if (ParentInventory.IsDragging)
            {
                var item = ParentInventory.DraggedItem;
                if (item.Type == ItemType.Equipment && item.EquipmentSlot == Slot)
                {
                    ParentInventory.EquipToSlot(item, Slot);
                    ParentInventory.EndDrag();
                }
            }
        }
    }

    #endregion
}

namespace GOFUS.Inventory
{
    public class InventoryItem
    {
        public int Id;
        public string Name;
        public int Quantity = 1;
        public ItemType Type;
        public EquipmentSlot EquipmentSlot;
        public bool IsStackable;
        public int MaxStack = 100;
        public float Weight;
        public int Value;
    }

    public enum ItemType
    {
        Weapon, Equipment, Consumable, Resource, Quest, Misc
    }

    public enum EquipmentSlot
    {
        Head, Chest, Legs, Feet, MainHand, OffHand,
        Ring1, Ring2, Amulet, Belt, Cape, Pet
    }

    public enum SortMode
    {
        None, Name, Value, Quantity, Type, Weight
    }
}