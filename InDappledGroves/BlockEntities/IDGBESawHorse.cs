using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Items.Tools;
using InDappledGroves.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
    class IDGBESawHorse : BlockEntityDisplay
    {
        //public bool isPaired { get; set; }
        public bool IsPaired;
        public bool IsConBlock { get; set; }
        public BlockPos ConBlockPos { get; set; }
        public BlockPos pairedBlockPos { get; set; }

        SawHorseRecipe recipe;

        readonly InventoryGeneric inv;
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "sawhorse";

        public IDGBESawHorse()
        {
            inv = new InventoryGeneric(2, "sawhorse-slot", null, null);
        }

        public ItemSlot InputSlot()
        {
            return inv[1];
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            CollectibleObject colObj = slot.Itemstack?.Collectible;


            if (Block.Variant["state"] == "compound" && !IsConBlock)
            {
                return (Api.World.BlockAccessor.GetBlockEntity(ConBlockPos) as IDGBESawHorse).OnInteract(byPlayer, blockSel);
            }

            //If players hand is empty, try to take item from sawhorse station
            if (slot.Empty)
            {
                if (TryTake(byPlayer))
                {
                    MarkDirty(true);
                    return true;
                }
            }

            else if (!slot.Empty && this.Inventory[1].Empty)
            {
                if (colObj.Attributes != null && DoesSlotMatchRecipe(slot))
                {

                    if (TryPut(slot))
                    {
                        this.Api.World.PlaySoundAt(GetSound(slot) ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                        MarkDirty(true);
                        return true;
                    }
                }
            }
            else if (colObj != null && colObj.HasBehavior<BehaviorWoodPlaning>() && !this.Inventory.Empty)
            {
                if (slot.Itemstack.Collectible is IDGTool tool)
                {
                    recipe = GetMatchingSawHorseRecipe(Inventory[1], tool.GetToolModeName(slot.Itemstack));
                    if (recipe != null)
                    {

                        byPlayer.Entity.StartAnimation("axechop");
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }


        public bool DoesSlotMatchRecipe(ItemSlot slots)
        {
            List<SawHorseRecipe> recipes = IDGRecipeRegistry.Loaded.SawHorseRecipes;
            if (recipes == null) return false;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(Api.World, slots))
                {
                    return true;
                }
            }

            return false;
        }

        public SawHorseRecipe GetMatchingSawHorseRecipe(ItemSlot slots, string curTMode)
        {

            List<SawHorseRecipe> recipes = IDGRecipeRegistry.Loaded.SawHorseRecipes;
            if (recipes == null) return null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(Api.World, slots) && curTMode == recipes[j].ToolMode)
                {
                    return recipes[j];
                }
            }

            return null;
        }

        private AssetLocation GetSound(ItemSlot slot)
        {
            if (slot.Itemstack == null)
            {
                return null;
            }
            else
            {
                Block block = slot.Itemstack.Block;
                if (block == null)
                {
                    return null;
                }
                else
                {
                    BlockSounds sounds = block.Sounds;
                    return (sounds?.Place);
                }
            }
        }

        private bool TryTake(IPlayer byPlayer)
        {
            if (!inv[1].Empty)
            {
                ItemStack stack = inv[1].TakeOutWhole();
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }
                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                updateMeshes();
                return true;
            }

            return false;
        }

        private bool TryPut(ItemSlot slot)
        {
            if (inv[1].Empty)
            {
                int moved = slot.TryPutInto(Api.World, inv[1]);
                return moved > 0;
            }
            updateMeshes();
            return false;
        }
        public void CreateSawhorseStation(BlockPos placedSawHorse)
        {
            IsPaired = true;
            IsConBlock = true;
            ConBlockPos = Pos;
            pairedBlockPos = placedSawHorse;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("ispaired", IsPaired);
            tree.SetBool("isconblock", IsConBlock);
            if (ConBlockPos != null)
            {
                tree.SetBlockPos("conblock", ConBlockPos);
            }
            else
            {
                ConBlockPos = null;
            }
            if (pairedBlockPos != null)
            {
                tree.SetBlockPos("pairedblock", pairedBlockPos);
            }
            else
            {
                pairedBlockPos = null;
            }

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            IsPaired = tree.GetBool("ispaired");
            IsConBlock = tree.GetBool("isconblock");
            ConBlockPos = tree.GetBlockPos("conblock", null);
            pairedBlockPos = tree.GetBlockPos("pairedblock", null);

            MarkDirty(true);
        }

        #region rendering
        public override void updateMeshes()
        {
            this.updateMesh(1);

            base.updateMeshes();
        }

        #region rendering
        protected ModelTransform genTransform(ItemStack stack)
        {
            ModelTransform transform;
            if (stack != null && stack.Collectible.Attributes["workStationTransforms"]["idgSawHorseProps"]["idgSawHorseTransform"].Exists)
            {
                transform = stack.Collectible.Attributes["workStationTransforms"]["idgSawHorseProps"]["idgSawHorseTransform"].AsObject<ModelTransform>();
            }
            else
            {
                transform = new ModelTransform
                {
                    Translation = new Vec3f(),
                    Rotation = new Vec3f(),
                    Origin = new Vec3f()
                };
            }

            transform.EnsureDefaultValues();
            return transform;
        }

        public float addRotate(string side, string axis)
        {
            JsonObject transforms = this.Inventory[1].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawHorseProps"]["idgSawHorseTransform"];
            return transforms["rotation"][side][axis].Exists ? transforms["rotation"][side][axis].AsFloat() : 0f;
        }

        public float addTranslate(string side, string axis)
        {
            JsonObject transforms = this.Inventory[1].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawHorseProps"]["idgSawHorseTransform"];
            return transforms["translation"][side][axis].Exists ? transforms["translation"][side][axis].AsFloat() : 0f;
        }

        public float addOrigin(string side, string axis)
        {
            JsonObject transforms = this.Inventory[1].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawHorseProps"]["idgSawHorseTransform"];
            return transforms["origin"][side][axis].Exists ? transforms["origin"][side][axis].AsFloat() : 0f;
        }
        public float addScale(string side)
        {
            JsonObject transforms = this.Inventory[1].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawHorseProps"]["idgSawHorseTransform"];
            return transforms["scale"][side].Exists ? transforms["scale"][side].AsFloat() : 1f;
        }
        #endregion

        public void SpawnOutput(SawHorseRecipe recipe, BlockPos pos)
        {
            int j = recipe.Output.StackSize;
            for (int i = j; i > 0; i--)
            {
                Api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
            }
        }

        #endregion

        public SawHorseRecipe GetMatchingPlaningRecipe(ItemSlot slots)
        {
            List<SawHorseRecipe> recipes = IDGRecipeRegistry.Loaded.SawHorseRecipes;
            if (recipes == null) return null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(Api.World, slots))
                {
                    return recipes[j];
                }
            }

            return null;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Block.Variant["state"] == "compound")
            {
                //Alter this code to produce an output based on the recipe that results from the held tool and its current mode.
                //If no tool is held, return only contents
                dsc.AppendLine("Contains " + (ConBlockPos != null && Api.World.BlockAccessor.GetBlockEntity(ConBlockPos) is IDGBESawHorse besawhorse ? besawhorse.inv[1].Empty ? "nothing" : besawhorse.inv[1].Itemstack.ToString() : "nothing"));
            }
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[1][];
            for (int index = 0; index < 1; index++)
            {
                    ItemStack itemstack = this.Inventory[index].Itemstack;
                    if (itemstack != null)
                    {
                        tfMatrices[index] = new Matrixf().Set(genTransform(itemstack).AsMatrix).Values;
                    }
            }
            return tfMatrices;
        }
    }
}