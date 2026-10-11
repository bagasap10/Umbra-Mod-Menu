using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;


namespace UmbraMenu.Menus
{
    public class Items : Menu
    {
        public const int InfiniteEquipmentButtonId = 4, SpawnItemsButtonId = 6, SpawnEquipmentButtonId = 7,
            RemoveItemsButtonId = 8, DropItemsButtonId = 9, DropInventoryButtonId = 10,
            StackButtonId = 11, ChestButtonId = 12, ClearButtonId = 13, PacksButtonId = 15;
        private static readonly IMenu items = new NormalMenu(3, new Rect(738, 10, 20, 20), "ITEMS MENU");
        public static bool isDropItemForAll, isDropItemFromInventory, noEquipmentCD, chestItemList;
        public bool IsDropItemForAll
        {
            get
            {
                return isDropItemForAll;
            }
            set
            {
                isDropItemForAll = value;
                if (isDropItemFromInventory && isDropItemForAll)
                {
                    isDropItemFromInventory = false;
                    toggleDropInvItems.SetEnabled(false);
                }
            }
        }
        public bool IsDropItemFromInventory
        {
            get
            {
                return isDropItemFromInventory;
            }
            set
            {
                isDropItemFromInventory = value;
                if (isDropItemFromInventory && isDropItemForAll)
                {
                    isDropItemForAll = false;
                    toggleDropItems.SetEnabled(false);
                }
            }
        }

        private readonly WeightedSelection<List<ItemIndex>> weightedSelection = BuildRollItemsDropTable();
        public static int itemsToRoll = 5;
        public int ItemsToRoll
        {
            get
            {
                return itemsToRoll;
            }
            set
            {
                itemsToRoll = InventorySafety.RollCount(value);
                rollItems.SetText($"ROLL ITEMS : {itemsToRoll}");
            }
        }
        public static int allItemsQuantity = 1;
        public int AllItemsQuantity
        {
            get
            {
                return allItemsQuantity;
            }
            set
            {
                allItemsQuantity = InventorySafety.BulkQuantity(value);
                giveAllItems.SetText($"GIVE ALL ITEMS : {allItemsQuantity}");
                giveAllNonLunarVoidItems?.SetText($"GIVE ALL ITEMS (NO LUNAR/VOID) : {allItemsQuantity}");
            }
        }

        public Button giveAllItems;
        public Button giveAllNonLunarVoidItems;
        public Button rollItems;
        public Button toggleItemListMenu;
        public Button toggleEquipmentListMenu;
        public Button toggleDropItems;
        public Button toggleDropInvItems;
        public Button toggleEquipmentCD;
        public Button stackInventory;
        public Button clearInventory;
        public Button toggleChestItemMenu;
        public Button toggleRemoveItemMenu;
        public Button togglePacksMenu;

        public Items() : base(items)
        {
            if (UmbraMenu.characterCollected)
            {
                void ToggleItemsListMenu() => UmbraMenu.menus[12].ToggleMenu();
                void ToggleEquipmentListMenu() => UmbraMenu.menus[13].ToggleMenu();
                void ToggleRemoveItemsMenu()
                {
                    UmbraMenu.inventoryItemListMenu.ToggleMenu();
                    if (UmbraMenu.inventoryItemListMenu.IsEnabled())
                        UmbraMenu.inventoryItemListMenu.RefreshInventory(true);
                }

                giveAllItems = new Button(new MulButton(this, 1, $"GIVE ALL ITEMS : {AllItemsQuantity}", GiveAllItems, IncreaseGiveAllQuantity, DecreaseGiveAllQuantity));
                giveAllNonLunarVoidItems = new Button(new MulButton(this,2 ,$"GIVE ALL ITEMS (NO LUNAR/VOID) : {AllItemsQuantity}",GiveAllNonLunarVoidItems,IncreaseGiveAllQuantity,DecreaseGiveAllQuantity));
                rollItems = new Button(new MulButton(this, 3, $"ROLL ITEMS : {ItemsToRoll}", RollItems, IncreaseItemsToRoll, DecreaseItemsToRoll));
                toggleItemListMenu = new Button(new TogglableButton(this, SpawnItemsButtonId, "ITEM SPAWN MENU : OFF", "ITEM SPAWN MENU : ON", ToggleItemsListMenu, ToggleItemsListMenu));
                toggleEquipmentListMenu = new Button(new TogglableButton(this, SpawnEquipmentButtonId, "EQUIPMENT SPAWN MENU : OFF", "EQUIPMENT SPAWN MENU : ON", ToggleEquipmentListMenu, ToggleEquipmentListMenu));
                toggleDropItems = new Button(new TogglableButton(this, DropItemsButtonId, "DROP ITEMS : OFF", "DROP ITEMS : ON", ToggleDrop, ToggleDrop, isDropItemForAll));
                toggleDropInvItems = new Button(new TogglableButton(this, DropInventoryButtonId, "DROP FROM INVENTORY : OFF", "DROP FROM INVENTORY : ON", ToggleDropFromInventory, ToggleDropFromInventory, isDropItemFromInventory));
                toggleEquipmentCD = new Button(new TogglableButton(this, InfiniteEquipmentButtonId, "INFINITE EQUIPMENT : OFF", "INFINITE EQUIPMENT : ON", ToggleEquipmentCD, ToggleEquipmentCD, noEquipmentCD));
                stackInventory = new Button(new NormalButton(this, StackButtonId, "STACK INVENTORY", StackInventory));
                clearInventory = new Button(new NormalButton(this, ClearButtonId, "CLEAR INVENTORY", ClearInventory));
                toggleChestItemMenu = new Button(new TogglableButton(this, ChestButtonId, "CHANGE CHEST ITEM : OFF", "CHANGE CHEST ITEM : ON", ToggleChestItemListMenu, ToggleChestItemListMenu));
                toggleRemoveItemMenu = new Button(new TogglableButton(this, InventoryItemList.ActivatingButtonId,
                    "REMOVE ITEMS : OFF", "REMOVE ITEMS : ON", ToggleRemoveItemsMenu, ToggleRemoveItemsMenu));
                void TogglePacksMenu() => UmbraMenu.menus[ItemPackList.MenuId].ToggleMenu();
                togglePacksMenu = new Button(new TogglableButton(this, PacksButtonId, "ITEM PACKS : OFF", "ITEM PACKS : ON", TogglePacksMenu, TogglePacksMenu));

                AddButtons(new List<Button>()
                {
                    giveAllItems,
                    giveAllNonLunarVoidItems,
                    rollItems,
                    toggleEquipmentCD,
                    new Button(new DividerButton(this, 5, "", UITheme.GlassRim, false)),
                    toggleItemListMenu,
                    toggleEquipmentListMenu,
                    toggleRemoveItemMenu,
                    toggleDropItems,
                    toggleDropInvItems,
                    stackInventory,
                    toggleChestItemMenu,
                    clearInventory,
                    new Button(new DividerButton(this, 14, "", UITheme.GlassRim, false)),
                    togglePacksMenu
                });
                SetActivatingButton(Utility.FindButtonById(0, 3));
                SetPrevMenuId(0);
            }
        }

        public override void Draw()
        {
            if (IsEnabled())
            {
                SetWindow();
                base.Draw();
            }
        }

        public override void Reset()
        {
            isDropItemForAll = false;
            isDropItemFromInventory = false;
            noEquipmentCD = false;
            chestItemList = false;
            itemsToRoll = 5;
            allItemsQuantity = 1;
            base.Reset();
        }

        #region Toggle cheat functions
        private void ToggleDrop()
        {
            IsDropItemForAll = !IsDropItemForAll;
        }

        private void ToggleDropFromInventory()
        {
            IsDropItemFromInventory = !IsDropItemFromInventory;
        }

        private void ToggleEquipmentCD()
        {
            noEquipmentCD = !noEquipmentCD;
        }



        private void ToggleChestItemListMenu() => UmbraMenu.menus[14].ToggleMenu();

        //private void ToggleChestItemListMenu()
        //{
        //    if (toggleChestItemMenu.IsEnabled())
        //    {
        //        ChestItemList.DisableChests();
        //        chestItemList = false;
        //    }
        //    else
        //    {
        //        ChestItemList.EnableChests();
        //        chestItemList = true;
        //    }
        //    UmbraMenu.menus[14].ToggleMenu();
        //}
        #endregion

        #region Drop Item Handle
        const Int16 HandleItemId = 99;

        class DropItemPacket : MessageBase
        {
            public GameObject Player;
            public ItemIndex ItemIndex;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(Player);
                writer.Write((UInt16)ItemIndex);
            }

            public override void Deserialize(NetworkReader reader)
            {
                Player = reader.ReadGameObject();
                ItemIndex = (ItemIndex)reader.ReadUInt16();
            }
        }

        static void SendDropItem(GameObject player, ItemIndex itemIndex)
        {
            NetworkServer.SendToAll(HandleItemId, new DropItemPacket
            {
                Player = player,
                ItemIndex = itemIndex
            });
        }

        [RoR2.Networking.NetworkMessageHandler(msgType = HandleItemId, client = true)]

        static void HandleDropItem(NetworkMessage netMsg)
        {
            var dropItem = netMsg.ReadMessage<DropItemPacket>();
            var body = dropItem.Player.GetComponent<CharacterBody>();

            PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(dropItem.ItemIndex);

            // New API requires UniquePickup
            UniquePickup uniquePickup = new UniquePickup(pickupIndex);

            Vector3 position = body.transform.position + Vector3.up * 1.5f;
            Vector3 velocity = Vector3.up * 20f + body.transform.forward * 2f;

            PickupDropletController.CreatePickupDroplet(uniquePickup,position,velocity,false);
        }

        //static void HandleDropItem(NetworkMessage netMsg)
        //{
        //    var dropItem = netMsg.ReadMessage<DropItemPacket>();
        //    var body = dropItem.Player.GetComponent<CharacterBody>();
        //    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(dropItem.ItemIndex), body.transform.position + Vector3.up * 1.5f, Vector3.up * 20f + body.transform.forward * 2f);
        //}

        public static void DropItemMethod(ItemIndex itemIndex)
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var networkClient = NetworkClient.allClients.FirstOrDefault();
            if (networkClient != null)
            {
                networkClient.RegisterHandlerSafe(HandleItemId, HandleDropItem);
            }
            SendDropItem(user.cachedBody.gameObject, itemIndex);
        }
        #endregion

        #region Drop Equipment Handle
        const Int16 HandleEquipmentId = 98;

        class DropEquipmentPacket : MessageBase
        {
            public GameObject Player;
            public EquipmentIndex EquipmentIndex;
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(Player);
                writer.Write((UInt16)EquipmentIndex);
            }

            public override void Deserialize(NetworkReader reader)
            {
                Player = reader.ReadGameObject();
                EquipmentIndex = (EquipmentIndex)reader.ReadUInt16();
            }
        }

        static void SendDropEquipment(GameObject player, EquipmentIndex equipmentIndex)
        {
            NetworkServer.SendToAll(HandleEquipmentId, new DropEquipmentPacket
            {
                Player = player,
                EquipmentIndex = equipmentIndex
            });
        }

        [RoR2.Networking.NetworkMessageHandler(msgType = HandleEquipmentId, client = true)]

        static void HandleDropEquipment(NetworkMessage netMsg)
        {
            var dropEquipment = netMsg.ReadMessage<DropEquipmentPacket>();
            var body = dropEquipment.Player.GetComponent<CharacterBody>();

            // Find PickupIndex for the equipment
            PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(dropEquipment.EquipmentIndex);

            // Wrap in UniquePickup (required for new API)
            UniquePickup uniquePickup = new UniquePickup(pickupIndex);

            Vector3 position = body.transform.position + Vector3.up * 1.5f;
            Vector3 velocity = Vector3.up * 20f + body.transform.forward * 2f;

            // Use NEW overload (4 params) — not obsolete
            PickupDropletController.CreatePickupDroplet(uniquePickup,position,velocity,false);
        }

        //static void HandleDropEquipment(NetworkMessage netMsg)
        //{
        //    var dropEquipment = netMsg.ReadMessage<DropEquipmentPacket>();
        //    var body = dropEquipment.Player.GetComponent<CharacterBody>();
        //    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(dropEquipment.EquipmentIndex), body.transform.position + Vector3.up * 1.5f, Vector3.up * 20f + body.transform.forward * 2f);
        //}

        public static void DropEquipmentMethod(EquipmentIndex equipmentIndex)
        {
            var user = RoR2.LocalUserManager.GetFirstLocalUser();
            var networkClient = NetworkClient.allClients.FirstOrDefault();
            if (networkClient != null)
            {
                networkClient.RegisterHandlerSafe(HandleEquipmentId, HandleDropEquipment);
            }
            SendDropEquipment(user.cachedBody.gameObject, equipmentIndex);
        }
        #endregion

        // Remove each permanent stack once; scan allies once after the inventory is cleared.
        public void ClearInventory()
        {
            var inventory = UmbraMenu.LocalPlayerInv;
            if (!inventory) return;
            foreach (ItemIndex itemIndex in CurrentInventory())
                inventory.RemoveItemPermanent(itemIndex, inventory.GetItemCountPermanent(itemIndex));
            inventory.SetEquipmentIndex(EquipmentIndex.None,
                new EquipmentLocation { slot = (uint)inventory.activeEquipmentSlot, set = 0 });
            if (NetworkServer.active)
            {
                var search = new BullseyeSearch { filterByLoS = false, maxDistanceFilter = float.MaxValue, maxAngleFilter = float.MaxValue };
                search.RefreshCandidates();
                foreach (var hurtbox in search.GetResults())
                {
                    var mob = HurtBox.FindEntityObject(hurtbox);
                    if (!mob || !mob.name.StartsWith("BeetleGuardAlly", StringComparison.Ordinal)) continue;
                    var health = mob.GetComponent<HealthComponent>();
                    if (health && health.alive) health.Suicide();
                }
            }
            if (UmbraMenu.characterCollected) Player.RemoveAllBuffs();
        }
        // random items
        public void RollItems()
        {
            try
            {
                if (!UmbraMenu.characterCollected || !UmbraMenu.LocalPlayerInv) return;
                int num = InventorySafety.RollCount(itemsToRoll);
                if (num > 0)
                {
                    for (int i = 0; i < num; i++)
                    {
                        List<ItemIndex> list = weightedSelection.Evaluate(UnityEngine.Random.value);
                        if (list == null || list.Count == 0) continue;
                        TryGive(list[UnityEngine.Random.Range(0, list.Count)], 1);
                    }
                }
            }
            catch (ArgumentException)
            {
            }
        }

        //Builds loot table for RollItems()
        public static WeightedSelection<List<ItemIndex>> BuildRollItemsDropTable()
        {
            WeightedSelection<List<ItemIndex>> weightedSelection = new WeightedSelection<List<ItemIndex>>(8);

            List<ItemIndex> tier1 = new List<ItemIndex>();
            List<ItemIndex> tier2 = new List<ItemIndex>();
            List<ItemIndex> tier3 = new List<ItemIndex>();
            List<ItemIndex> lunar = new List<ItemIndex>();
            List<ItemIndex> boss = UmbraMenu.bossItems;

            foreach (ItemIndex itemIndex in ItemCatalog.tier1ItemList)
            {
                tier1.Add(itemIndex);
            }
            foreach (ItemIndex itemIndex in ItemCatalog.tier2ItemList)
            {
                tier2.Add(itemIndex);
            }
            foreach (ItemIndex itemIndex in ItemCatalog.tier3ItemList)
            {
                tier3.Add(itemIndex);
            }
            foreach (ItemIndex itemIndex in ItemCatalog.lunarItemList)
            {
                lunar.Add(itemIndex);
            }




            weightedSelection.AddChoice(tier1, 63.5f);
            weightedSelection.AddChoice(tier2, 27f);
            weightedSelection.AddChoice(tier3, 3.5f);
            weightedSelection.AddChoice(boss, 3.5f);
            weightedSelection.AddChoice(lunar, 2.5f);
            return weightedSelection;
        }

        //Gives all items
        public void GiveAllItems()
        {
            if (UmbraMenu.LocalPlayerInv)
            {
                foreach (ItemIndex itemIndex in UmbraMenu.items)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (!itemDef) continue;
                    string itemName = itemDef.name;


                    //plantonhit kills you when you pick it up
                    if (itemName == "PlantOnHit" || itemName == "HealthDecay" || itemName == "TonicAffliction" || itemName == "BurnNearby" || itemName == "CrippleWardOnLevel" || itemName == "Ghost" || itemName == "ExtraLifeConsumed")
                        continue;
                    //UmbraMenu.LocalPlayerInv.GiveItem(itemIndex, allItemsQuantity);
                    TryGive(itemIndex, InventorySafety.BulkQuantity(allItemsQuantity));
                }
            }
        }

        // Gives all items except Lunar & Void items
        public void GiveAllNonLunarVoidItems()
        {
            if (UmbraMenu.LocalPlayerInv)
            {
                foreach (ItemIndex itemIndex in UmbraMenu.items)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (!itemDef) continue;
                    string itemName = itemDef.name;

                    // Skip “dangerous” items like the original function
                    if (itemName == "PlantOnHit" || itemName == "HealthDecay" ||
                        itemName == "TonicAffliction" || itemName == "BurnNearby" ||
                        itemName == "CrippleWardOnLevel" || itemName == "Ghost" ||
                        itemName == "ExtraLifeConsumed")
                        continue;

                    // Exclude Lunar & Void items based on tier color
                    var tierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                    if (tierDef != null)
                    {
                        if (tierDef.colorIndex == ColorCatalog.ColorIndex.LunarItem ||
                            tierDef.colorIndex == ColorCatalog.ColorIndex.VoidItem)
                        {
                            continue;
                        }
                    }

                    TryGive(itemIndex, InventorySafety.BulkQuantity(allItemsQuantity));
                }
            }
        }


        //Does the same thing as the shrine of order. Orders all your items into stacks of several random items.
        public void StackInventory()
        {
            if (!UmbraMenu.LocalPlayerInv || !Run.instance) return;
            UmbraMenu.LocalPlayerInv.ShrineRestackInventory(Run.instance.runRNG);
        }

        internal static bool TryGive(ItemIndex itemIndex, int quantity)
        {
            var inventory = UmbraMenu.LocalPlayerInv;
            if (!inventory || !ItemCatalog.GetItemDef(itemIndex)) return false;
            if (!InventorySafety.CanAdd(inventory.GetItemCountPermanent(itemIndex), quantity))
            {
                Chat.AddMessage("<color=yellow>Item stack limit reached. Remove items before adding more.</color>");
                return false;
            }
            inventory.GiveItemPermanent(itemIndex, quantity);
            return true;
        }

        //Sets equipment cooldown to 0 if its on cooldown
        public static void NoEquipmentCooldown()
        {
            if (!UmbraMenu.characterCollected || !UmbraMenu.LocalPlayerInv || !Run.instance) return;
            //EquipmentState equipment = UmbraMenu.LocalPlayerInv.GetEquipment((uint)UmbraMenu.LocalPlayerInv.activeEquipmentSlot);
            
            // Updated to use the two-parameter overload per the obsoletion message.
            // Pass 0 as the equipment set (default).
            var location = new EquipmentLocation { slot = (uint)UmbraMenu.LocalPlayerInv.activeEquipmentSlot, set = 0 };
            EquipmentState equipment = UmbraMenu.LocalPlayerInv.GetEquipment(location);

            if (equipment.chargeFinishTime != Run.FixedTimeStamp.zero)
            {
                EquipmentState newState = new EquipmentState(
                    equipment.equipmentIndex,
                    Run.FixedTimeStamp.zero,
                    equipment.charges
                );

                // NEW valid overload (3 parameters)
                UmbraMenu.LocalPlayerInv.SetEquipment(
                    newState,
                    location
                );
            }
        }

        //Gets players current items to make sure they can drop item from inventory if enabled
        public static List<ItemIndex> CurrentInventory()
        {
            var currentInventory = new List<ItemIndex>();
            if (!UmbraMenu.LocalPlayerInv) return currentInventory;

            foreach (ItemIndex itemIndex in ItemCatalog.allItems)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef)
                {
                    //int itemCount = UmbraMenu.LocalPlayerInv.GetItemCount(itemIndex);
                    int itemCount = UmbraMenu.LocalPlayerInv.GetItemCountPermanent(itemIndex);
                    if (itemCount > 0) // If item is in inventory
                    {
                        currentInventory.Add(itemIndex); // add to list
                    }
                }
            }
            return currentInventory;
        }

        #region Increase/Decrease Value Actions
        public void IncreaseItemsToRoll()
        {
            if (ItemsToRoll < InventorySafety.MaximumRollCount)
                ItemsToRoll += 5;
        }

        public void IncreaseGiveAllQuantity()
        {
            if (AllItemsQuantity < InventorySafety.MaximumBulkQuantity)
                AllItemsQuantity += 1;
        }

        public void DecreaseItemsToRoll()
        {
            if (ItemsToRoll > 5)
                ItemsToRoll -= 5;
        }

        public void DecreaseGiveAllQuantity()
        {
            if (AllItemsQuantity > 1)
                AllItemsQuantity -= 1;
        }
        #endregion
    }
}
