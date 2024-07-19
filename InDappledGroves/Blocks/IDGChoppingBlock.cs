using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Config;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
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
			ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			if (collObj != null && collObj.HasBehavior<BehaviorIDGTool>()) { 
				curTMode = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack); 
				toolModeMod = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack); 
			};
			          
			if (!bechoppingblock.Inventory.Empty)
			{
				if (collObj != null && collObj.HasBehavior<BehaviorIDGTool>())
				{
					recipe = bechoppingblock.GetMatchingChoppingBlockRecipe(world, bechoppingblock.InputSlot, curTMode);
					if (recipe != null)
					{
						resistance = (bechoppingblock.Inventory[0].Itemstack.Collectible is Block ? bechoppingblock.Inventory[0].Itemstack.Block.Resistance : bechoppingblock.Inventory[0].Itemstack.Collectible.Attributes["resistance"].AsFloat() * InDappledGroves.baseWorkstationResistanceMult);
							byPlayer.Entity.StartAnimation("axesplit-fp");
							return true;
					}
					return false;
				}
			}

			return bechoppingblock.OnInteract(byPlayer);
		}
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            CollectibleObject chopTool = itemstack?.Collectible;
            BlockPos position = blockSel.Position;
            
            if (chopTool != null && chopTool.HasBehavior<BehaviorIDGTool>() 
                && world.BlockAccessor.GetBlockEntity(blockSel.Position) is IDGBEChoppingBlock idgbechoppingblock 
                && !idgbechoppingblock.Inventory.Empty)
            {
                idgbechoppingblock.updateMeshes();
                idgbechoppingblock.MarkDirty(true, null);

                if (this.playNextSound < secondsUsed)
                {
                    this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
                    this.playNextSound += 0.7f;
                }

                //if (playNextSound < secondsUsed)
                //{
                //    api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), position.X, position.Y, position.Z, byPlayer, true, 32, 1f);
                //    playNextSound += 0.7f;
                //}

                if (idgbechoppingblock.Inventory[0].Itemstack.Collectible is Block)
                {
                    curDmgFromMiningSpeed += (chopTool.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, idgbechoppingblock.Inventory[0].Itemstack.Block, byPlayer) * InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
                }
                else
                {
                    curDmgFromMiningSpeed += (chopTool.MiningSpeed[(EnumBlockMaterial)recipe.IngredientMaterial] * InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
                }

                lastSecondsUsed = secondsUsed;

                EntityPlayer playerEntity = byPlayer.Entity;

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
