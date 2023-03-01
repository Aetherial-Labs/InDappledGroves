namespace InDappledGroves.BlockEntities
{
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Util;
    using global::InDappledGroves.Util;
    using global::InDappledGroves.Blocks;
    using global::InDappledGroves.WorldGen;

    public class BETreeHollowGrown : BlockEntityDisplayCase, ITexPositionSource
    {
        private readonly int maxSlots = 8;
        public override string InventoryClassName => "treehollowgrown";


        
        private readonly double updateMinutes = InDappledGrovesConfig.Current.TreeHollowsUpdateMinutes;
        private long updateTick;

        //private const int MinItems = 1;
        //private const int MaxItems = 8;

        public override InventoryBase Inventory { get; }

        public BETreeHollowGrown()
        {
            this.Inventory = new InventoryGeneric(this.maxSlots, null, null);
        }



        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                var updateTick = this.RegisterGameTickListener(this.TreeHollowUpdate, (int)(this.updateMinutes * 60000));
            }
        }

        public void TreeHollowUpdate(float par)
        {
            if (this.Inventory[0].Empty)
            {
                //var uf = new TreeHollows();
                this.MarkDirty();
            }
        }


        internal bool OnInteract(IPlayer byPlayer)
        {
            if (this.Api.Side.IsServer()) //reset the listener on interact
            {
                this.UnregisterGameTickListener(this.updateTick);
                this.updateTick = this.RegisterGameTickListener(this.TreeHollowUpdate, (int)(this.updateMinutes * 60000));
            }

            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (playerSlot.Empty)
            {
                if (this.TryTake(byPlayer))
                { return true; }
                return false;
            }

            return false;
        }

        internal void OnBreak()
        {
            for (var index = this.maxSlots - 1; index >= 0; index--)
            {
                if (!this.Inventory[index].Empty)
                {
                    var stack = this.Inventory[index].TakeOut(1);
                    if (stack.StackSize > 0)
                    { this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5)); }
                    this.MarkDirty(true);
                }
            }
        }

        private int LastFilledSlot()
        {
            var slot = this.maxSlots - 1;
            var found = false;
            do
            {
                if (!this.Inventory[slot].Empty)
                { found = true; }
                else
                { slot--; }
            }
            while (slot > -1 && found == false);
            return slot;
        }


        private bool TryTake(IPlayer byPlayer)
        {
            var index = this.LastFilledSlot();
            if (index == -1)
            { return false; }

            var stack = this.Inventory[index].TakeOut(1);
            if (byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                var sound = stack.Block?.Sounds?.Place;
                this.Api.World.PlaySoundAt(sound ?? new AssetLocation("game:sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
            }

            if (stack.StackSize > 0)
            {
                this.Api.World.SpawnItemEntity(stack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            this.MarkDirty(true);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            if (this.Api != null)
            {
                if (this.Api.Side == EnumAppSide.Client)
                { this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos); }
            }
        }

        public override void updateMeshes()
        {
            var index = this.LastFilledSlot() + 1;
            if (index == 0)
            { return; } //inventory empty
            for (var slot = 0; slot < index; slot++)
            {
                if (!this.Inventory[slot].Empty)
                {
                    var stack = this.Inventory[slot].Itemstack;
                    if (stack?.Item?.Shape != null)
                    {
                        if ((stack.Collectible as ItemWearable) == null)
                        {
                            this.updateMesh(slot);
                        }
                    }
                }
            }
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            if (this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) is not BlockTreeHollowGrown block)
            { return base.OnTesselation(mesher, tesselator); }
            mesh = this.capi.TesselatorManager.GetDefaultBlockMesh(block); //add tree hollow
            mesher.AddMeshData(mesh);

            //only render what is in the highest inventory slot
            var index = this.LastFilledSlot();
            if (index > -1)
            {
                var stack = this.Inventory[index].Itemstack;
                if (stack != null)
                {
                    if (stack.Class == EnumItemClass.Block)
                    {
                        mesh = this.capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                    }
                    else
                    {
                        this.nowTesselatingObj = stack.Item;
                        if (stack?.Item?.Shape != null)
                        {
                            if ((stack.Collectible as ItemWearable) == null)
                            {
                                this.capi.Tesselator.TesselateItem(stack.Item, out mesh, this);
                                if (mesh != null)
                                {
                                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                                }
                            }
                        }
                        else
                        {
                            // seeds prolly
                            var shapeBase = "indappledgroves:shapes/";
                            var shapePath = "block/trapbait"; //baited (for now)
                            var texture = tesselator.GetTexSource(block);
                            mesh = block.GenMesh(this.Api as ICoreClientAPI, shapeBase + shapePath, texture, tesselator);
                            if (mesh != null)
                            {
                                mesh.Translate(new Vec3f(0f, 0.03f, 0f));
                            }
                        }
                    }
                    if (mesh != null)
                    {
                        if (block.FirstCodePart(1).Contains("base"))
                        { mesh.Translate(new Vec3f(0f, 0.03f, 0f)); }
                        else //up has thicker bottom
                        { mesh.Translate(new Vec3f(0f, 0.23f, 0f)); }
                        if (block.LastCodePart() == "north" || block.LastCodePart() == "south")
                        {
                            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 90, 0);
                        }
                        mesher.AddMeshData(mesh);
                    }
                }
            }
            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            //base.GetBlockInfo(forPlayer, sb);
            var index = this.LastFilledSlot();
            if (index == -1)
            { sb.AppendLine(Lang.Get("Empty")); }
            else
            {
                var desc = this.Inventory[index].Itemstack.GetName();
                sb.AppendLine(Lang.Get(desc));
            }
            sb.AppendLine();
            if (forPlayer?.CurrentBlockSelection == null)
            { return; }
        }
    }
}