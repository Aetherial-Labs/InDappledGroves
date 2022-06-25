using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.BlockEntities
{
    class IDGBESawHorse : BlockEntityDisplay
    {
        public bool isPaired { get; set; }
        public bool isConBlock { get; set; }
        public BlockPos conBlock { get; set; }

        public override InventoryBase Inventory { get; }

        public override string InventoryClassName => "sawhorse";

        public IDGBESawHorse()
        {
            Inventory = new InventoryGeneric(1, "sawbuck-slot", null, null);
            meshes = new MeshData[0];
        }

        internal bool OnInteract(IPlayer byPlayer)
        {
            Api.Logger.Debug("OnInteract IDGBESawhorse Reached at BlockPos: " + Pos.X + " " + Pos.Y + " " + Pos.Z + ".");
            return false;
        }
    }
}
