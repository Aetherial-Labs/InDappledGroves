using InDappledGroves.BlockEntities;
using InDappledGroves.Blocks;
using InDappledGroves.Util;
using InDappledGroves.Items.Tools;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using InDappledGroves.CollectibleBehaviors;

namespace InDappledGroves
{
    public class InDappledGroves : ModSystem
    {
        ICoreClientAPI capi;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            //Register Items
            api.RegisterItemClass("idgaxe", typeof(ItemIDGAxe));
            api.RegisterItemClass("idgsaw", typeof(ItemIDGSaw));

            //Register Blocks
            api.RegisterBlockClass("idgchoppingblock", typeof(IDGChoppingBlock));
            api.RegisterBlockClass("idgsawbuck", typeof(IDGSawBuck));
            api.RegisterBlockClass("idgsawhorse", typeof(IDGSawHorse));

            //Register BlockEntities
            api.RegisterBlockEntityClass("idgbechoppingblock", typeof(IDGBEChoppingBlock));
            api.RegisterBlockEntityClass("idgbesawbuck", typeof(IDGBESawBuck));
            api.RegisterBlockEntityClass("idgbesawhorse", typeof(IDGBESawHorse));


            //Register CollectibleBehaviors
            api.RegisterCollectibleBehaviorClass("WoodSplitter", typeof(BehaviorWoodSplitter));
            api.RegisterCollectibleBehaviorClass("WoodSawer", typeof(BehaviorWoodSawer));
            

            //Check for Existing Config file, create one if none exists
            try
            {
                var Config = api.LoadModConfig<InDappledGrovesConfig>("indappledgroves.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    InDappledGrovesConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    InDappledGrovesConfig.Current = InDappledGrovesConfig.GetDefault();
                }
            }
            catch
            {
                InDappledGrovesConfig.Current = InDappledGrovesConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(InDappledGrovesConfig.Current, "indappledgroves.json");
            }
        }
    }
}
