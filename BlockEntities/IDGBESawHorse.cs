using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.BlockEntities
{
    class IDGBESawHorse : BlockEntityDisplay
    {
        public bool isPaired { get; set; }
        public bool isConBlock { get; set; }
        public BlockPos conBlock { get; set; }
        public BlockPos pairedBlock { get; set; }

        readonly InventoryGeneric inv;
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "sawhorse";

        public IDGBESawHorse()
        {
            inv = new InventoryGeneric(2, "sawhorse-slot", null, null);
            meshes = new MeshData[1];
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            CollectibleObject colObj = slot.Itemstack?.Collectible;
            bool isPlaningBox = blockSel.SelectionBoxIndex == 1;
            //bool isPlanable = (colObj == null || colObj.Attributes == null) ? false : colObj.Attributes["planable"].AsBool();
            
            if (!isPlaningBox) return false;
            if (slot.Empty)
            {
                if (TryTake(byPlayer))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            else if (!slot.Empty && slot.Itemstack.Collectible.FirstCodePart() == "plank")
            {
                if (TryPut(slot))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            else if (slot.Itemstack.Collectible.FirstCodePart() == "axe")
            {
                if (!inv[1].Empty)
                {
                    ItemStack stack = inv[1].TakeOutWhole();
                    stack.StackSize = 2;
                    if (byPlayer.InventoryManager.TryGiveItemstack(stack));
                }
            }
            return false;
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
                    updateMeshes();
                    return moved > 0;
                }
            return false;
        }

        internal bool CreateSawhorseStation(BlockSelection blockSel, BlockPos placePosition)
        {
            isPaired = true;
            isConBlock = true;
            conBlock = Pos;
            pairedBlock = placePosition;
            return false;
        }

        //protected override MeshData genMesh(ItemStack stack)
        //{

        //    IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
        //    MeshData meshData;
        //    if (containedMeshSource != null)
        //    {
        //        meshData = containedMeshSource.GenMesh(stack, this.capi.BlockTextureAtlas, this.Pos);
        //        meshData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, base.Block.Shape.rotateY * 0.017453292f, 0f);
        //    }
        //    else
        //    {
        //        this.nowTesselatingObj = stack.Collectible;
        //        this.nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
        //        capi.Tesselator.TesselateItem(stack.Item, out meshData, this);
        //    }
        //    ModelTransform transform = stack.Collectible.Attributes.AsObject<ModelTransform>();
        //    transform.EnsureDefaultValues();
        //    transform.Rotation.X = 0;
        //    transform.Rotation.Y = block.Shape.rotateY;
        //    transform.Rotation.Z = 0;
        //    meshData.ModelTransform(transform);

        //    return meshData;
        //}

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            
            tree.SetBool("ispaired", isPaired);
            tree.SetBool("isconblock", isConBlock);
            if (conBlock != null)
                tree.SetBlockPos("conblock", conBlock);
            if (pairedBlock != null)
                tree.SetBlockPos("pairedblock", pairedBlock);
            base.ToTreeAttributes(tree);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            tree.GetBool("ispaired");
            tree.GetBool("isconblock");
            tree.GetBlockPos("conblock", null);
            tree.GetBlockPos("pairedblock", null);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("My Pos is " + Pos);
            dsc.AppendLine("conBlock is " + conBlock);
            dsc.AppendLine("pairedBlock is " + pairedBlock);
            dsc.AppendLine("isConBlock is " + isConBlock);
            dsc.AppendLine("isPaired is " + isPaired);
            dsc.AppendLine("Contains " + (conBlock != null && Api.World.BlockAccessor.GetBlockEntity(conBlock) is IDGBESawHorse fish ? fish.inv[1].Empty ? "nothing" : fish.inv[1].Itemstack.ToString() : "nothing"));
            base.GetBlockInfo(forPlayer, dsc);
        }
    }
}
