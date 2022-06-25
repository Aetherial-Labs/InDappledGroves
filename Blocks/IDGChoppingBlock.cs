using InDappledGroves.BlockEntities;
using InDappledGroves.Util;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGChoppingBlock : Block
    {
		SplittingRecipe recipe;
		// Token: 0x06000BD6 RID: 3030 RVA: 0x000068EB File Offset: 0x00004AEB
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

        #region Original Code
        //      public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        //{
        //	ItemStack chopToolStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        //	CollectibleObject chopCollObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

        //	//Check to see if block entity exists
        //          if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) return base.OnBlockInteractStart(world, byPlayer, blockSel);

        //	//If player is holding something, it has the BehaviorWoodSplitter behavior, and the chopping block is not empty.
        //	if (chopCollObj != null && chopCollObj.HasBehavior<BehaviorWoodSplitter>() 
        //		&& !bechoppingblock.Inventory.Empty)
        //          {
        //		if (chopToolStack.Attributes.GetInt("durability") < chopCollObj.GetBehavior<BehaviorWoodSplitter>().choppingBlockChopDamage && InDappledGrovesConfig.Current.preventChoppingWithLowDurability) {
        //			(api as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", chopCollObj.GetBehavior<BehaviorWoodSplitter>().choppingBlockChopDamage));
        //			return base.OnBlockInteractStart(world, byPlayer, blockSel); 
        //		}

        //		byPlayer.Entity.StartAnimation("axechop");
        //		return true;
        //          }

        //	//Call the block entity OnInteract
        //	return bechoppingblock.OnInteract(byPlayer);
        //}
        #endregion

        #region RevisedCode
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack chopToolStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject chopCollObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			//If player is holding something, it has the BehaviorWoodSplitter behavior, and the chopping block is not empty.
			if (chopCollObj != null && chopCollObj.HasBehavior<BehaviorWoodSplitter>() && !bechoppingblock.Inventory.Empty) {
				recipe = GetMatchingSplittingRecipe(world, bechoppingblock.InputSlot);
				if (recipe != null && chopToolStack.Attributes.GetInt("durability") < chopCollObj.GetBehavior<BehaviorWoodSplitter>().choppingBlockChopDamage && InDappledGrovesConfig.Current.preventChoppingWithLowDurability)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", chopCollObj.GetBehavior<BehaviorWoodSplitter>().choppingBlockChopDamage));
					return base.OnBlockInteractStart(world, byPlayer, blockSel);
				}

				byPlayer.Entity.StartAnimation("axechop");
				return true;
			}

			//Call the block entity OnInteract
			return bechoppingblock.OnInteract(byPlayer);
		}
        #endregion

        public SplittingRecipe GetMatchingSplittingRecipe(IWorldAccessor world, ItemSlot slots)
		{
			List<SplittingRecipe> recipes = IDGRecipeRegistry.Loaded.SplittingRecipes;
			if (recipes == null) return null;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(api.World, slots))
				{
					return recipes[j];
				}
			}

			return null;
		}
		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
			CollectibleObject chopTool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
			BlockPos pos = blockSel.Position;

			if (chopTool != null && chopTool.HasBehavior<BehaviorWoodSplitter>() && !bechoppingblock.Inventory.Empty)
			{
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
                }
                if (secondsUsed >= chopTool.GetBehavior<BehaviorWoodSplitter>().choppingBlockChopTime)
                {

					chopTool.GetBehavior<BehaviorWoodSplitter>().SpawnOutput(bechoppingblock.Inventory[0].Itemstack.Collectible, 
					byPlayer.Entity, blockSel.Position, chopTool.GetBehavior<BehaviorWoodSplitter>().choppingBlockChopDamage);

          bechoppingblock.Inventory.Clear();
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock).updateMeshes();
					bechoppingblock.MarkDirty(true);
                }		
				return !bechoppingblock.Inventory.Empty;
			}
			return false;
        }
		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			playNextSound = 0.7f;
			byPlayer.Entity.StopAnimation("axechop");
		}

		private float playNextSound;
	}
		
}
