using InDappledGroves.Interfaces;
using InDappledGroves.Util;
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
    class BehaviorWoodSawing : CollectibleBehavior, IBehaviorVariant
    {
        ICoreClientAPI capi;
        public InventoryBase Inventory { get; }
        public string InventoryClassName => "worldinventory";
        public SkillItem[] toolModes;

        public GroundRecipe recipe;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public BehaviorWoodSawing(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            Inventory = new InventoryGeneric(1, "sawingtool-slot", null, null);
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes ?? new SkillItem[] { null };
        }
        public override void OnLoaded(ICoreAPI api)
        {
            this.capi = (api as ICoreClientAPI);



            this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "idgSawModes", delegate
            {

                SkillItem[] array;
                array = new SkillItem[]
                {
                        new SkillItem
                        {
                            Code = new AssetLocation("sawing"),
                            Name = Lang.Get("Sawing", Array.Empty<object>())
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

