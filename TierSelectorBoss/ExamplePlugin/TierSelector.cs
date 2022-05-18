using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace TierSelector
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
        public const string PluginVersion = "1.2";


        public void Awake()
        {
            Log.Init(Logger);

            On.RoR2.Run.BuildDropTable += Run_BuildDropTable;
            On.RoR2.ChestBehavior.Roll += ChestBehavior_Roll;
            On.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehavior_AddShrineStack;
            On.RoR2.MultiShopController.CreateTerminals += MultiShopController_CreateTerminals;
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
                    Log.LogInfo("Found drop table.");
                    // Copy because we dont want to change it forever.

                    pickupIndex = self.dropTable.GenerateDrop(self.rng);
                    if (pickupIndex != PickupIndex.none || !Run.instance.availableTier3DropList.Contains(pickupIndex))
                    {
                        // force green.
                        pickupIndex = Run.instance.availableBossDropList[UnityEngine.Random.Range(0, Run.instance.availableBossDropList.Count)];
                    }
                    Log.LogInfo("Oh god.");

                }
            }
            else
            {
                PickupIndex none = PickupIndex.none;
                PickupIndex value = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                PickupIndex value2 = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                PickupIndex value3 = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableBossDropList);
                PickupIndex value4 = self.rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>(8);
                weightedSelection.AddChoice(none, self.failureWeight);
                //weightedSelection.AddChoice(value, self.tier1Weight);
                //weightedSelection.AddChoice(value2, self.tier2Weight);
                weightedSelection.AddChoice(value3, self.tier3Weight);
                //weightedSelection.AddChoice(value4, self.equipmentWeight);
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

            // TODO: Invoke global static.
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
            var items2 = FindObjectsOfType<PurchaseInteraction>();

            foreach (var item in items)
            {
                if (item.gameObject.name.Contains("Duplicator"))
                {
                    foreach (var components in item.gameObject.GetComponents<PurchaseInteraction>())
                    {
                        components.costType = CostTypeIndex.BossItem;
                        //Log.LogInfo(components.GetScriptClassName());
                    }
                }

                item.selfGeneratePickup = false;
                Log.LogInfo(item.gameObject.name);
                item.SetPickupIndex(Run.instance.availableBossDropList[UnityEngine.Random.Range(0, Run.instance.availableBossDropList.Count)], false);
            }
            Log.LogInfo("You da bomb");
        }

        private void ChestBehavior_Roll(On.RoR2.ChestBehavior.orig_Roll orig, ChestBehavior self)
        {
            orig(self);
            Log.LogInfo("WOOOOOP");
            self.dropPickup = Run.instance.availableBossDropList[UnityEngine.Random.Range(0, Run.instance.availableBossDropList.Count)];
        }

        private void Run_BuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            Log.LogInfo("Forcing ");
            self.smallChestDropTierSelector.Clear();
            self.smallChestDropTierSelector.AddChoice(self.availableBossDropList, 1.0f);

            self.mediumChestDropTierSelector.Clear();
            self.mediumChestDropTierSelector.AddChoice(self.availableBossDropList, 1.0f);

            self.largeChestDropTierSelector.Clear();
            self.largeChestDropTierSelector.AddChoice(self.availableBossDropList, 1.0f);


        }



        //The Update() method is run on every frame of the game.
        private void Update()
        {
            //This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
            }
        }
    }
}
