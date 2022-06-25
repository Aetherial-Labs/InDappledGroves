using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves
{
    class BehaviorWoodSplitter : CollectibleBehavior
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);          
        }

        public BehaviorWoodSplitter(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);
            this.groundChopTime = collObj.Attributes["woodSplitterProps"]["groundChopTime"].AsInt(4);
            this.choppingBlockChopTime = collObj.Attributes["woodSplitterProps"]["choppingBlockChopTime"].AsInt(2);
            this.groundChopDamage = collObj.Attributes["woodSplitterProps"]["groundChopDamage"].AsInt(4);
            this.choppingBlockChopDamage = collObj.Attributes["woodSplitterProps"]["choppingBlockChopDamage"].AsInt(2);
            interactions = ObjectCacheUtil.GetOrCreate(api, "idgaxeInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "indappledgroves:itemhelp-axe-chopwood",
                            HotKeyCode = "sprint",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            woodParticles = InitializeWoodParticles();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            
            //-- Do not process the chopping action if the player is not holding ctrl, or no block is selected --//
            if (!byEntity.Controls.Sprint || blockSel == null)
                return;

            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            JsonObject attributes = interactedBlock.Attributes?["idgChoppingBlockProps"]["cuttable"];
            if (attributes == null || !attributes.Exists || !attributes.AsBool(false)) return;
            api.Logger.Debug("This fired.");
            if (slot.Itemstack.Attributes.GetInt("durability") < groundChopDamage)
            {
                api.Logger.Debug("This internal fired.");
                capi.TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", groundChopDamage));
                return;
            }
            byEntity.StartAnimation("axechop");
            
            playNextSound = 0.25f;

            handHandling = EnumHandHandling.PreventDefault;
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            BlockPos pos = blockSel.Position;
            if (blockSel != null)
            {

                if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                {
                    api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                    playNextSound += .7f;
                }
                if (secondsUsed >= groundChopTime)
                {
                   
                    Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (secondsUsed >= groundChopTime && interactedBlock.Attributes["idgChoppingBlockProps"]["cuttable"].AsBool(false))
                    SpawnOutput(new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position)).Collectible, byEntity, pos, groundChopDamage);
                    api.World.BlockAccessor.SetBlock(0, blockSel.Position);
                    return false;
                }

            }
            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            byEntity.StopAnimation("axechop");
        }

        //-- Spawns firewood when chopping cycle is finished --//
        public void SpawnOutput(CollectibleObject chopObj, EntityAgent byEntity, BlockPos pos, int dmg)
        {
                Item itemOutput = api.World.GetItem(new AssetLocation(chopObj.Attributes["idgChoppingBlockProps"]["output"]["code"].AsString()));
                Block blockOutput = api.World.GetBlock(new AssetLocation(chopObj.Attributes["idgChoppingBlockProps"]["output"]["code"].AsString()));

                int quantity = chopObj.Attributes["idgChoppingBlockProps"]["output"]["quantity"].AsInt();

                for (int i = quantity; i > 0; i--)
                {
                        api.World.SpawnItemEntity(new ItemStack(itemOutput!=null?itemOutput:blockOutput), pos.ToVec3d() + new Vec3d(0, .25, 0));
                }

                if (byEntity is EntityPlayer player)
                player.RightHandItemSlot.Itemstack.Collectible.DamageItem(api.World, byEntity, player.RightHandItemSlot, groundChopDamage);

        }

        public void SpawnOutput(SplittingRecipe recipe, EntityAgent byEntity, BlockPos pos, int dmg)
        {
            Item output = recipe.Output.ResolvedItemstack.Item;
            int quantity = recipe.Output.Quantity;
            for (int i = quantity; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(output), pos.ToVec3d() + new Vec3d(0, .25, 0));
            }

            if (byEntity is EntityPlayer player)
                player.RightHandItemSlot.Itemstack.Collectible.DamageItem(api.World, byEntity, player.RightHandItemSlot, groundChopDamage);

        }

        private SimpleParticleProperties InitializeWoodParticles()
        {
            return new SimpleParticleProperties()
            {
                MinPos = new Vec3d(),
                AddPos = new Vec3d(),
                MinQuantity = 0,
                AddQuantity = 3,
                Color = ColorUtil.ToRgba(100, 200, 200, 200),
                GravityEffect = 1f,
                WithTerrainCollision = true,
                ParticleModel = EnumParticleModel.Quad,
                LifeLength = 0.5f,
                MinVelocity = new Vec3f(-1, 2, -1),
                AddVelocity = new Vec3f(2, 0, 2),
                MinSize = 0.07f,
                MaxSize = 0.1f,
                WindAffected = true
            };
        }

        static readonly SimpleParticleProperties dustParticles = new()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            Color = ColorUtil.ToRgba(100, 200, 200, 200),
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
            WindAffected = true
        };

        private void SetParticleColourAndPosition(int colour, Vec3d minpos)
        {
            SetParticleColour(colour);

            woodParticles.MinPos = minpos;
            woodParticles.AddPos = new Vec3d(1, 1, 1);
        }

        private void SetParticleColour(int colour)
        {
            woodParticles.Color = colour;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return interactions;
        }

        public int groundChopTime;
        public int choppingBlockChopTime;
        public int groundChopDamage;
        public int choppingBlockChopDamage;
        WorldInteraction[] interactions = null;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}
