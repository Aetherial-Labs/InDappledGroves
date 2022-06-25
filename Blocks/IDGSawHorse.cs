using InDappledGroves.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGSawHorse : Block
    {

        //public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        //{
        //    base.OnBlockPlaced(world, blockPos, byItemStack);
        //}
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBESawHorse besawhorse) return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (besawhorse.isPaired) return false;
            if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode)) besawhorse.conBlock = blockSel.Position;
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!playerSlot.Empty && playerSlot.Itemstack.Collectible is IDGSawHorse)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBESawHorse besawhorse) return base.OnBlockInteractStart(world, byPlayer, blockSel);
                if (besawhorse.isPaired)
                {
                    System.Diagnostics.Debug.WriteLine("This Block is Paired");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("This Block is Now Paired");
                    besawhorse.isPaired = true;
                    System.Diagnostics.Debug.WriteLine("This Block is Now A ConBlock");
                    besawhorse.isConBlock = true;
                    besawhorse.conBlock = blockSel.Position;
                    System.Diagnostics.Debug.WriteLine("This Blocks Pos is " + besawhorse.conBlock);
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

	}
}
