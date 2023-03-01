using InDappledGroves.Interfaces;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.CollectibleBehaviors
{
    class BehaviorWoodPlaning : CollectibleBehavior, IBehaviorVariant
    {
        private ICoreClientAPI capi;
        public SkillItem[] toolModes;
        public InventoryBase Inventory { get; }
        public string InventoryClassName => "worldinventory";

        public GroundRecipe recipe;
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
           
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes ?? new SkillItem[] { null };
        }

        public BehaviorWoodPlaning(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            Inventory = new InventoryGeneric(1, "planingtool-slot", null, null);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.capi = (api as ICoreClientAPI);
            //interactions = ObjectCacheUtil.GetOrCreate(api, "idgplaneInteractions", () =>
            //{
            //    return new WorldInteraction[] {
            //        new WorldInteraction()
            //            {
            //                ActionLangCode = "indappledgroves:itemhelp-tool-planewood",
            //                MouseButton = EnumMouseButton.Right
            //            },
            //        };
            //});

            this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "idgPlaningModes", delegate
            {

                SkillItem[] array;
                array = new SkillItem[]
                {
                        new SkillItem
                        {
                            Code = new AssetLocation("planing"),
                            Name = Lang.Get("Planing", Array.Empty<object>())
                        }
                };

                if (capi != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("indappledgroves:textures/icons/" + array[i].Code.FirstCodePart().ToString() + ".svg"), 48, 48, 5, new int?(-1)));
                        array[i].TexturePremultipliedAlpha = false;
                    }
                }

                return array;
            });
        }

    }
}

