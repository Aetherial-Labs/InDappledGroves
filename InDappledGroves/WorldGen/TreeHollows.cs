using System;
using System.Collections.Generic;

using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using InDappledGroves.Util;
using InDappledGroves.BlockEntities;
using InDappledGroves.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;

namespace InDappledGroves.WorldGen
{

    public class TreeHollows : ModSystem
    {

        private ICoreServerAPI sapi; //The main interface we will use for interacting with Vintage Story
        private int chunkSize; //Size of chunks. Chunks are cubes so this is the size of the cube.
        private ISet<string> treeTypes; //Stores tree types that will be used for detecting trees for placing our tree hollows
        private ISet<string> stumpTypes; //Stores tree types that will be used for detecting trees for placing our tree hollows
        private IBlockAccessor chunkGenBlockAccessor; //Used for accessing blocks during chunk generation
        private IBlockAccessor worldBlockAccessor; //Used for accessing blocks after chunk generation

        private readonly string[] dirs = { "north", "south", "east", "west" };
        private readonly List<string> woods = new();
        private readonly List<string> stumps = new();

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            
            this.worldBlockAccessor = api.World.BlockAccessor;
            this.chunkSize = this.worldBlockAccessor.ChunkSize;

            woods.AddRange(InDappledGrovesConfig.Current.woodTypes);
            stumps.AddRange(InDappledGrovesConfig.Current.stumpTypes);
            this.treeTypes = new HashSet<string>();
            this.stumpTypes = new HashSet<string>();
            this.LoadTreeTypes(this.treeTypes);
            this.LoadStumpTypes(this.stumpTypes);

            //Registers our command with the system's command registry.
            //1.17 disable /hollow
            this.sapi.RegisterCommand("hollow", "Place a tree hollow with random items", "", this.PlaceTreeHollowInFrontOfPlayer, Privilege.controlserver);

            //Registers a delegate to be called so we can get a reference to the chunk gen block accessor
            this.sapi.Event.GetWorldgenBlockAccessor(this.OnWorldGenBlockAccessor);
            //Registers a delegate to be called when a chunk column is generating in the Vegetation phase of generation
            this.sapi.Event.ChunkColumnGeneration(this.OnChunkColumnGeneration, EnumWorldGenPass.PreDone, "standard");
            this.sapi.Event.ChunkColumnLoaded += Event_ChunkColumnLoaded;
            //this.sapi.Event.ChunkDirty += Event_ChunkDirty;
            this.sapi = api;
        }

        private void Event_ChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (InDappledGrovesConfig.Current.RunTreeGenOnChunkReload)
            {
                foreach (IWorldChunk chunk in chunks)
                {
                    if (chunk.Empty /*|| chunk.GetModdata<bool>("hasIDGLoaded", false) == true*/) continue;

                    IMapChunk mc = sapi.World.BlockAccessor.GetMapChunk(chunkCoord);
                    if (mc == null) continue;   //this chunk isn't actually loaded, no need to examine it.

                    if (chunk.GetModdata<bool>("hasIDGLoaded", false) == true) break;

                    runTreeGen(new BlockPos(chunkCoord.X, 0, chunkCoord.Y));
                    chunk.SetModdata<bool>("hasIDGLoaded", true);
                }
            }
        }

        //Our mod only needs to be loaded by the server
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        private void LoadTreeTypes(ISet<string> treeTypes)
        {
            //var treeTypesFromFile = this.sapi.Assets.TryGet("worldproperties/block/wood.json").ToObject<StandardWorldProperty>();
            foreach (var variant in this.woods)
            {
                treeTypes.Add($"log-grown-" + variant + "-ud");
            }
        }

        private void LoadStumpTypes(ISet<string> stumpTypes)
        {
            //var treeTypesFromFile = this.sapi.Assets.TryGet("worldproperties/block/wood.json").ToObject<StandardWorldProperty>();
            foreach (var variant in this.stumps)
            {
                stumpTypes.Add($"log-grown-" + variant + "-ud");
            }
        }

        /// <summary>
        /// Stores the chunk gen thread's IBlockAccessor for use when generating tree hollows during chunk gen. This callback
        /// is necessary because chunk loading happens in a separate thread and it's important to use this block accessor
        /// when placing tree hollows during chunk gen.
        /// </summary>
        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
        {
            this.chunkGenBlockAccessor = chunkProvider.GetBlockAccessor(true);
        }




        /// <summary>
        /// Called when a number of chunks have been generated. For each chunk we first determine if we should place a tree hollow
        /// and if we should we then loop through each block to find a tree. When one is found we place the block.
        /// </summary>
        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkgenparams)
        {
            //Debug.WriteLine("Entering the death loop for chunk " + chunkX + " " + chunkZ);
            for (var i = 0; i < chunks.Length; i++)
            {
                runTreeGen(new BlockPos(chunkX,0,chunkZ));
                chunks[i].SetModdata<bool>("hasIDGLoaded", true);
            }
        }

        private void runTreeGen(BlockPos pos)
        {
            var hollowsPlacedCount = 0;

            var blockPos = new BlockPos();
            //arbitrarily limit x axis scan for performance reasons (/4)
            for (var x = 0; x < this.chunkSize; x++)
            {
                //arbitrarily limit z axis scan for performance reasons (/4)
                for (var z = 0; z < this.chunkSize; z++)
                {
                    blockPos.X = (pos.X * this.chunkSize) + x;
                    blockPos.Z = (pos.Z * this.chunkSize) + z;
                    blockPos.Y = this.worldBlockAccessor.GetTerrainMapheightAt(blockPos) + 1;
                    Block curBlock = this.chunkGenBlockAccessor.GetBlock(blockPos, BlockLayersAccess.Default);
                    if (sapi.ModLoader.IsModEnabled("primitivesurvival"))
                    {
                        if (IsStumpLog(curBlock)) {
                            PlaceTreeStump(this.chunkGenBlockAccessor, blockPos);
                        }
                        continue;
                    } 

                    if (!IsStumpLog(curBlock) || this.worldBlockAccessor.GetBlock(blockPos.DownCopy()).Fertility > 0) continue;
                    if (hollowsPlacedCount < InDappledGrovesConfig.Current.TreeHollowsMaxPerChunk && (sapi.World.Rand.NextDouble() < 0.2))
                    {
                        var hollowWasPlaced = this.PlaceTreeHollow(this.chunkGenBlockAccessor, blockPos);
                        if (hollowWasPlaced)
                        {
                            hollowsPlacedCount++;
                            continue;
                        }
                    }
                    PlaceTreeStump(this.chunkGenBlockAccessor, blockPos);



                    if (ShouldPlaceHollow() && hollowsPlacedCount < InDappledGrovesConfig.Current.TreeHollowsMaxPerChunk && IsTreeLog(curBlock))
                    {
                        var hollowLocation = this.TryGetHollowLocation(blockPos);
                        if (hollowLocation == null) continue;
                        var hollowWasPlaced = this.PlaceTreeHollow(this.chunkGenBlockAccessor, hollowLocation);
                        if (hollowWasPlaced)
                        {
                            hollowsPlacedCount++;
                        }
                    }

                }

            }
        }        

        // Returns the location to place the hollow if the given world coordinates is a tree, null if it's not a tree.
        private BlockPos TryGetHollowLocation(BlockPos pos)
        {
            var block = this.chunkGenBlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (this.IsTreeLog(block))
            {
                for (var posY = pos.Y; posY >= 0; posY--)
                {
                    while (pos.Y-- > 0)
                    {
                        var underBlock = this.chunkGenBlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
                        if (this.IsTreeLog(underBlock))
                        {
                            continue;
                        }
                        return pos.UpCopy();
                    }
                }
            }
            return null;
        }

        private bool IsTreeLog(Block block)
        {
            return this.treeTypes.Contains(block.Code.Path);
        }

        private bool IsStumpLog(Block block)
        {
            return this.stumpTypes.Contains(block.Code.Path);
        }

        // Delegate for /hollow command. Places a treehollow 2 blocks in front of the player
        private void PlaceTreeHollowInFrontOfPlayer(IServerPlayer player, int groupId, CmdArgs args)
        {
            this.PlaceTreeHollow(this.sapi.World.BlockAccessor, player.Entity.Pos.HorizontalAheadCopy(2).AsBlockPos);
        }

        // Places a tree stump at the given world coordinates using the given IBlockAccessor
        private bool PlaceTreeStump(IBlockAccessor blockAccessor, BlockPos pos)
        {

            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            //Debug.WriteLine("Will replace:" + treeBlock.Code.Path);
            var stumpType = "pine";

            if (treeBlock.FirstCodePart() == "log")
            {
                stumpType = treeBlock.FirstCodePart(2);
            }

            var withPath = "indappledgroves:treestump-grown-" + stumpType + "-" + this.dirs[this.sapi.World.Rand.Next(4)];
            //Debug.WriteLine("With: " + withPath);
            var withBlockID = this.sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            blockAccessor.SetBlock(0, pos);
            if (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null)) return true;
            return false;
        }

        // Places a tree hollow filled with random items at the given world coordinates using the given IBlockAccessor
        private bool PlaceTreeHollow(IBlockAccessor blockAccessor, BlockPos pos)
        {
            //Moved this to chunk gen to hopefully speed things up...a lot
            /*
            if (!this.ShouldPlaceHollow())
            {
                //Debug.WriteLine("cancelled!");
                return true;
            }
            */

            //consider moving it upwards
            var upCount = this.sapi.World.Rand.Next(2,8);
            var upCandidateBlock = blockAccessor.GetBlock(pos.UpCopy(upCount), BlockLayersAccess.Default);

            if (upCandidateBlock.FirstCodePart() == "log")
            { pos = pos.UpCopy(upCount); }

            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            //Debug.WriteLine("Will replace:" + treeBlock.Code.Path);
            var woodType = "pine";

            if (treeBlock.FirstCodePart() == "log")
            {
                woodType = treeBlock.FirstCodePart(2);
            }

            var hollowType = "up";
            if (this.sapi.World.Rand.Next(2) == 1)
            { hollowType = "up2"; }
            var belowBlock = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Default);
            if (belowBlock.Fertility > 0) //fertile ground below?
            {
                if (this.sapi.World.Rand.Next(2) == 1)
                { hollowType = "base"; }
                else
                { hollowType = "base2"; }
            }

            var withPath = "indappledgroves:treehollowgrown-" + hollowType + "-" + woodType + "-" + this.dirs[this.sapi.World.Rand.Next(4)];
            //Debug.WriteLine("With: " + withPath);
            var withBlockID = this.sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            if (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null))
            {
                var block = blockAccessor.GetBlock(pos, BlockLayersAccess.Default) as BlockTreeHollowGrown;
                if (block.EntityClass != null)
                {
                    if (block.EntityClass == withBlock.EntityClass)
                    {
                        blockAccessor.SpawnBlockEntity(block.EntityClass, pos);
                        var be = blockAccessor.GetBlockEntity(pos);
                        if (be is BETreeHollowGrown)
                        {
                            var hollow = blockAccessor.GetBlockEntity(pos) as BETreeHollowGrown;
                            ItemStack[] lootStacks = ConvertTreeLoot(block.Attributes["treeLoot"].AsArray(), pos);
                            if (lootStacks != null) AddItemStacks(hollow, lootStacks);
                        }
                    }
                }
                return true;
            }
            else
            { return false; }
        }

        private bool ShouldPlaceHollow()
        {
            var randomNumber = this.sapi.World.Rand.Next(0, 100);
            return randomNumber > 0 && randomNumber <= InDappledGrovesConfig.Current.TreeHollowsSpawnProbability * 100;
        }

        //Adds the given list of ItemStacks to the first slots in the given hollow.
        public void AddItemStacks(IBlockEntityContainer hollow, ItemStack[] itemStacks)
        {
            var slotNumber = 0;
            if (itemStacks != null)
            {

                while (slotNumber < sapi.World.Rand.Next(hollow.Inventory.Count - 1))
                {
                    var slot = hollow.Inventory[slotNumber];
                    slot.Itemstack = itemStacks[sapi.World.Rand.Next(0, itemStacks.Length - 1)];
                    slotNumber++;
                }
            }
        }

        private ItemStack[] ConvertTreeLoot(JsonObject[] treeLoot, BlockPos pos)
        {
            List<ItemStack> lootList = null;
            foreach (JsonObject lootStack in treeLoot)
            {

                TreeLootObject obj = new(lootStack);
                if (lootList == null && ClimateLootFilter(obj, pos))
                {
                    obj.bstack.Resolve(sapi.World, "treedrop: ", obj.bstack.Code);
                    lootList = new();
                    lootList.Add(obj.bstack.GetNextItemStack());
                    continue;
                }
                if (lootList != null && ClimateLootFilter(obj, pos) && lootList.Count >= 0 && lootList.Count <= 8)
                {
                    obj.bstack.Resolve(sapi.World, "treedrop: ", obj.bstack.Code);

                    lootList.Add(obj.bstack.GetNextItemStack());
                }
            }

            return lootList?.ToArray();
        }

        private bool ClimateLootFilter(TreeLootObject obj, BlockPos pos)
        {
            ClimateCondition local = sapi.World.BlockAccessor.GetClimateAt(pos);

            return (local.ForestDensity >= obj.cReqs.minForest)
            && (local.ForestDensity <= obj.cReqs.maxForest)
            && (local.ShrubDensity >= obj.cReqs.minShrub)
            && (local.ShrubDensity <= obj.cReqs.maxShrub)
            && (local.Rainfall >= obj.cReqs.minRain)
            && (local.Rainfall <= obj.cReqs.maxRain)
            && (local.Temperature >= obj.cReqs.minTemperature)
            && (local.Temperature <= obj.cReqs.maxTemperature)
            && ((((int)sapi.World.Calendar.GetSeason(pos)) == obj.cReqs.season) ||
                obj.cReqs.season == 4);
        }

    }
    internal class TreeLootObject
    {
        public BlockDropItemStack bstack { get; }
        public ClimateRequirements cReqs { get; }

        public TreeLootObject() { }
        public TreeLootObject(JsonObject treeLoot)
        {
            bstack = treeLoot["dropStack"].AsObject<BlockDropItemStack>();
            cReqs = treeLoot["dropReqs"].AsObject<ClimateRequirements>();

        }
    }

    internal class ClimateRequirements
    {
        public float minForest { get; set; } = 0.0f;
        public float maxForest { get; set; } = 1f;
        public float minRain { get; set; } = 0.0f;

        public float maxRain { get; set; } = 1f;
        public float minShrub { get; set; } = 0.0f;
        public float maxShrub { get; set; } = 1f;
        public float minTemperature { get; set; } = -100.0f;
        public float maxTemperature { get; set; } = 200f;
        public int season { get; set; } = 4;
        public ClimateRequirements() { }

    }
}