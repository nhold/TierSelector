using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Toasted
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(LobbyConfigAPI))]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class TierSelector : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Vorkiblo";
        public const string PluginName = "TierSelector";
        public const string PluginVersion = "1.3.0";

        public List<PickupIndex> currentLootTable;
        public CostTypeIndex currentCostType;

        public void Awake()
        {
            Log.Init(Logger);

            On.RoR2.Run.BuildDropTable += Run_BuildDropTable;
            On.RoR2.ChestBehavior.Roll += ChestBehavior_Roll;
            On.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehavior_AddShrineStack;
            On.RoR2.MultiShopController.CreateTerminals += MultiShopController_CreateTerminals;
            On.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
            Toasted.Config.Initialise(this);
        }

        private void BossGroup_DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            int participatingPlayerCount = Run.instance.participatingPlayerCount;
            if (participatingPlayerCount != 0 && self.dropPosition)
            {
                PickupIndex pickupIndex = PickupIndex.none;
                if (self.dropTable)
                {
                    pickupIndex = self.dropTable.GenerateDrop(self.rng);
                }
                else
                {
                    List<PickupIndex> list = Run.instance.availableTier2DropList;
                    if (self.forceTier3Reward)
                    {
                        list = Run.instance.availableTier3DropList;
                    }
                    pickupIndex = self.rng.NextElementUniform<PickupIndex>(list);
                }
                int num = 1 + self.bonusRewardCount;
                if (self.scaleRewardsByPlayerCount)
                {
                    num *= participatingPlayerCount;
                }
                float angle = 360f / (float)num;
                Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                int i = 0;
                while (i < num)
                {
                    PickupIndex pickupIndex2 = pickupIndex;
                    if ((self.bossDrops.Count > 0 || self.bossDropTables.Count > 0) && self.rng.nextNormalizedFloat <= self.bossDropChance)
                    {
                        if (self.bossDropTables.Count > 0)
                        {
                            pickupIndex2 = self.rng.NextElementUniform<PickupDropTable>(self.bossDropTables).GenerateDrop(self.rng);
                        }
                        else
                        {
                            pickupIndex2 = self.rng.NextElementUniform<PickupIndex>(self.bossDrops);
                        }
                    }

                    if (pickupIndex2 != PickupIndex.none || !currentLootTable.Contains(pickupIndex2))
                    {
                        // If it's outside of our loot table, we just generate from our selected one.
                        pickupIndex2 = currentLootTable[UnityEngine.Random.Range(0, currentLootTable.Count)];
                    }

                    PickupDropletController.CreatePickupDroplet(pickupIndex2, self.dropPosition.position, vector);
                    i++;
                    vector = rotation * vector;
                }
            }
        }

        private void ShrineChanceBehavior_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineChanceBehavior::AddShrineStack(RoR2.Interactor)' called on client");
                return;
            }

            PickupIndex pickupIndex = PickupIndex.none;
            if (self.dropTable)
            {
                if (self.rng.nextNormalizedFloat > self.failureChance)
                {

                    pickupIndex = self.dropTable.GenerateDrop(self.rng);

                    // Check here if it's a failure or if it's outside of our current loot table.
                    if (pickupIndex != PickupIndex.none || !currentLootTable.Contains(pickupIndex))
                    {
                        // If it's outside of our loot table, we just generate from our selected one.
                        pickupIndex = currentLootTable[UnityEngine.Random.Range(0, currentLootTable.Count)];
                    }
                }
            }
            else
            {
                // Otherwise just go with the flow!
                PickupIndex none = PickupIndex.none;
                PickupIndex selectedItem = self.rng.NextElementUniform<PickupIndex>(currentLootTable);
                WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>(8);
                weightedSelection.AddChoice(none, self.failureWeight);
                weightedSelection.AddChoice(selectedItem, self.tier3Weight);

                pickupIndex = weightedSelection.Evaluate(self.rng.nextNormalizedFloat);
            }

            bool flag = pickupIndex == PickupIndex.none;
            string baseToken;

            if (flag)
            {
                baseToken = "SHRINE_CHANCE_FAIL_MESSAGE";
            }
            else
            {
                baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE";
                self.successfulPurchaseCount++;
                PickupDropletController.CreatePickupDroplet(pickupIndex, self.dropletOrigin.position, self.dropletOrigin.forward * 20f);
            }

            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
                baseToken = baseToken
            });

            // TODO: Invoke global static using reflection.
            /*var type = typeof(ShrineChanceBehavior);
            foreach(var info in type.GetField("onShrineChancePurchaseGlobal"))

            ShrineChanceBehavior.onShrineChancePurchaseGlobal?.Invoke(flag, activator);*/

            self.waitingForRefresh = true;
            self.refreshTimer = 2f;
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = base.transform.position,
                rotation = Quaternion.identity,
                scale = 1f,
                color = self.shrineColor
            }, true);
            if (self.successfulPurchaseCount >= self.maxPurchaseCount)
            {
                self.symbolTransform.gameObject.SetActive(false);
            }
        }

        private void MultiShopController_CreateTerminals(On.RoR2.MultiShopController.orig_CreateTerminals orig, MultiShopController self)
        {
            orig(self);


            var items = FindObjectsOfType<ShopTerminalBehavior>();
            foreach (var item in items)
            {
                if (item.gameObject.name.Contains("Duplicator"))
                {
                    foreach (var components in item.gameObject.GetComponents<PurchaseInteraction>())
                    {
                        components.costType = currentCostType;
                    }
                }

                item.selfGeneratePickup = false;

                item.SetPickupIndex(currentLootTable[UnityEngine.Random.Range(0, currentLootTable.Count)], false);
            }
        }

        private void ChestBehavior_Roll(On.RoR2.ChestBehavior.orig_Roll orig, ChestBehavior self)
        {
            orig(self);

            self.dropPickup = currentLootTable[UnityEngine.Random.Range(0, currentLootTable.Count)];
        }

        private void Run_BuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            SetupCurrentLootTables();

            self.smallChestDropTierSelector.Clear();
            self.smallChestDropTierSelector.AddChoice(currentLootTable, 1.0f);

            self.mediumChestDropTierSelector.Clear();
            self.mediumChestDropTierSelector.AddChoice(currentLootTable, 1.0f);

            self.largeChestDropTierSelector.Clear();
            self.largeChestDropTierSelector.AddChoice(currentLootTable, 1.0f);
        }

        private void SetupCurrentLootTables()
        {
            currentLootTable = new List<PickupIndex>();

            Logger.LogInfo("Current selected tier is: " + Toasted.Config.selectedTier.Value.ToString());

            if (Toasted.Config.selectedTier.Value == Toasted.Config.ItemType.White)
            {
                currentLootTable = Run.instance.availableTier1DropList;
                currentCostType = CostTypeIndex.WhiteItem;
            }

            if (Toasted.Config.selectedTier.Value == Toasted.Config.ItemType.Green)
            {
                currentLootTable = Run.instance.availableTier2DropList;
                currentCostType = CostTypeIndex.GreenItem;
            }

            if (Toasted.Config.selectedTier.Value == Toasted.Config.ItemType.Red)
            {
                currentLootTable = Run.instance.availableTier3DropList;
                currentCostType = CostTypeIndex.RedItem;
            }

            if (Toasted.Config.selectedTier.Value == Toasted.Config.ItemType.Boss)
            {
                var newOne = ItemCatalog.FindItemIndex("Pearl");
                var newOne2 = ItemCatalog.FindItemIndex("ShinyPearl");
                var newOne3 = ItemCatalog.FindItemIndex("TitanGoldDuringTP");


                currentLootTable = new List<PickupIndex>(Run.instance.availableBossDropList);
                currentLootTable.Add(new PickupIndex(newOne));
                currentLootTable.Add(new PickupIndex(newOne2));
                currentLootTable.Add(new PickupIndex(newOne3));

                currentCostType = CostTypeIndex.BossItem;
            }

            if (Toasted.Config.selectedTier.Value == Toasted.Config.ItemType.Lunar)
            {
                currentLootTable = Run.instance.availableLunarCombinedDropList;
                currentLootTable.AddRange(Run.instance.availableLunarEquipmentDropList);
                currentCostType = CostTypeIndex.LunarItemOrEquipment;
            }

            if (Toasted.Config.selectedTier.Value == Toasted.Config.ItemType.Void)
            {
                currentLootTable = Run.instance.availableVoidTier1DropList;
                currentLootTable.AddRange(Run.instance.availableVoidTier2DropList);
                currentLootTable.AddRange(Run.instance.availableVoidTier3DropList);
                currentCostType = CostTypeIndex.TreasureCacheVoidItem;
            }
        }
    }
}
