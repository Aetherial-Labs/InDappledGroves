namespace InDappledGroves.BlockEntities
{
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.GameContent;
    using Vintagestory.API.Datastructures;
    using global::InDappledGroves.Util.Config;
    using Vintagestory.API.Server;
    using System;

    public class BETreeHollowGrown : BlockEntityDisplayCase, ITexPositionSource
    {
        private int Slots = 8;
        public override string InventoryClassName => "treehollowgrown";
        private ICoreClientAPI capi;
        private ICoreServerAPI sapi;

        public override InventoryBase Inventory { get; }

        public BETreeHollowGrown()
        {
            this.Inventory = new InventoryGeneric(Slots, null, null);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            return false;
            return base.OnTesselation(mesher, tessThreadTesselator);
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                sapi = api as ICoreServerAPI;
            } else
            {
                capi = api as ICoreClientAPI;
            }
        }

        internal bool OnInteract(IPlayer byPlayer)
        {

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
            for (var index = this.Slots - 1; index >= 0; index--)
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
            var slot = this.Slots - 1;
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