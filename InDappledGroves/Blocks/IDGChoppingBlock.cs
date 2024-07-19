using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Config;
using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGChoppingBlock : Block
    {

		
		ChoppingBlockRecipe recipe;
		float toolModeMod;
        bool recipecomplete = false;
        
        public override string GetHeldItemName(ItemStack stack) => GetName();
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
            string curTMode = "";
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

            /*
             * Ensure the collObj is not null.
             * Ensure BlockEntity at blockSel.Position is IDGBEChoppingBlock, 
             * and set it as the value of * bechoppingblock if so.
             */
			if (collObj == null || world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) 
                return base.OnBlockInteractStart(world, byPlayer, blockSel);

            /*
             * Determine if the held item is has 
             * behavior IDGtool,
             * Set values of curTMode and ToolModeMod if so.
             */
            SetToolValues(collObj, curTMode, slot);

            
            if (!bechoppingblock.Inventory.Empty && collObj.HasBehavior<BehaviorIDGTool>())
			{
                /*
                 * Check to see if the chopping block is empty
                 * If so, attempt to get a recipe for the item 
                 * on the block and set the resistance.
                 */
                bool recipeResistanceSet = CheckForRecipeAndSetResistance(byPlayer, world, bechoppingblock, curTMode);
                if (recipeResistanceSet)
                {
                    /* 
                     * If successful, start animation.
                     */
                    byPlayer.Entity.StartAnimation("axesplit-fp");
                }
                /*
                 * Return the value of recipeResistanceSet
                 */
                return recipeResistanceSet;

            }
            /*
             * If chopping block is empty or the collObj is not a tool, trigger OnInteract on the Block Entity.
             */
			return bechoppingblock.OnInteract(byPlayer);
		}

       

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            CollectibleObject chopTool = itemstack?.Collectible;
            BlockPos position = blockSel.Position;
            EntityPlayer playerEntity = byPlayer.Entity;

            if (chopTool != null && chopTool.HasBehavior<BehaviorIDGTool>() 
                && world.BlockAccessor.GetBlockEntity(blockSel.Position) is IDGBEChoppingBlock idgbechoppingblock 
                && !idgbechoppingblock.Inventory.Empty)
            {
                playWorkSound(secondsUsed,blockSel.Position, byPlayer);

                if (idgbechoppingblock.Inventory[0].Itemstack.Collectible is Block)
                {
                    curDmgFromMiningSpeed += (chopTool.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, idgbechoppingblock.Inventory[0].Itemstack.Block, byPlayer)
                        * InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
                }
                else
                {
                    curDmgFromMiningSpeed += (chopTool.MiningSpeed[(EnumBlockMaterial)recipe.IngredientMaterial] * InDappledGroves.baseWorkstationMiningSpdMult)
                        * (secondsUsed - lastSecondsUsed);
                }

                lastSecondsUsed = secondsUsed;

                float curMiningProgress = (secondsUsed + (curDmgFromMiningSpeed)) * (toolModeMod * IDGToolConfig.Current.baseWorkstationMiningSpdMult);
                float curResistance = resistance * IDGToolConfig.Current.baseWorkstationResistanceMult;

                if (curMiningProgress >= curResistance)
                {
                    if (api.Side == EnumAppSide.Server)
                    {
                        idgbechoppingblock.SpawnOutput(this.recipe, playerEntity, blockSel.Position);
                        chopTool.DamageItem(api.World, playerEntity, playerEntity.RightHandItemSlot, recipe.BaseToolDmg);
                        if (recipe.ReturnStack.ResolvedItemstack.Collectible.FirstCodePart() == "air")
                        {
                            idgbechoppingblock.Inventory.Clear();
                        }
                        else
                        {
                            idgbechoppingblock.Inventory.Clear();
                            idgbechoppingblock.ReturnStackPut(recipe.ReturnStack.ResolvedItemstack.Clone());

                        }
                        (world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock).updateMeshes();
                        idgbechoppingblock.MarkDirty(true, null);
                        byPlayer.Entity.StopAnimation("axesplit-fp");
                        
                        return false;
                    }
                    
                }
                return !idgbechoppingblock.Inventory.Empty;
            }
            return false;
        }

        private void playWorkSound(float secondsUsed, BlockPos position, IPlayer byPlayer)
        {
            if (this.playNextSound < secondsUsed)
            {
                this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
                this.playNextSound += 0.7f;
            }
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			resistance = 0;
			lastSecondsUsed = 0;
			curDmgFromMiningSpeed = 0;
			playNextSound = 0.7f;
            IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
            bechoppingblock.MarkDirty(true);
            bechoppingblock.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
			
		}

        /// <summary>
        /// Attempts to find a matching recipe and if found,
        /// if found set the resistance of Inventory[0].
        /// Returns bool value indicating success or failure.
        /// </summary>
        /// <param name="byPlayer"></param>
        /// <param name="world"></param>
        /// <param name="bechoppingblock"></param>
        /// <param name="curTMode"></param>
        /// <returns>boolean</returns>
        public bool CheckForRecipeAndSetResistance(IPlayer byPlayer, IWorldAccessor world, IDGBEChoppingBlock bechoppingblock, string curTMode)
        {
            recipe = bechoppingblock.GetMatchingChoppingBlockRecipe(world, bechoppingblock.InputSlot, curTMode);
            if (recipe != null)
            {
                /*
                 * If a recipe is found set the resistance value.
                 */
                resistance = (bechoppingblock.Inventory[0].Itemstack.Collectible is Block ?
                    bechoppingblock.Inventory[0].Itemstack.Block.Resistance :
                    bechoppingblock.Inventory[0].Itemstack.Collectible.Attributes["resistance"].AsFloat())
                    * InDappledGroves.baseWorkstationResistanceMult;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determine if received collObj has BehaviorIDGTool,
        /// if so, set curTMode to current tool mode of collObj,
        /// set Tool Mode Modifier to appropriate value.
        /// </summary>
        /// <param name="collObj"></param>
        /// <param name="curTMode"></param>
        /// <param name="slot"></param>
        private void SetToolValues(CollectibleObject collObj, string curTMode, ItemSlot slot)
        {
            if (collObj.HasBehavior<BehaviorIDGTool>())
            {
                curTMode = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack);
                toolModeMod = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack);
            };
        }

        public string GetName()
        {
            var material = Variant["wood"];
            var part = Lang.Get("material-" + $"{material}");
            part = $"{part[0].ToString().ToUpper()}{part.Substring(1)}";
            return string.Format($"{part} {Lang.Get("indappledgroves:block-choppingblock")}");
        }

        private float playNextSound;
		private float resistance;
		private float lastSecondsUsed;
		private float curDmgFromMiningSpeed;
		}
		
}
