using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Interfaces;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves
{
    class BehaviorWoodChopping : CollectibleBehavior, IBehaviorVariant
    { 
        ICoreAPI api;
        ICoreClientAPI capi;
        public BehaviorWoodChopping(CollectibleObject collObj) : base(collObj)
        {

            this.collObj = collObj;
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes ?? new SkillItem[] { null };
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);

            this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "idgAxeChopModes", delegate
            {

                SkillItem[] array;
                array = new SkillItem[]
                {
                        new SkillItem
                        {
                            Code = new AssetLocation("chopping"),
                            Name = Lang.Get("Chopping", Array.Empty<object>())
                        }
                };

                if (capi != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("indappledgroves:textures/icons/" + array[i].Code.FirstCodePart().ToString() + ".svg"), 48, 48, 5, new int?(-1)));
                        array[i].TexturePremultipliedAlpha = false;
                    }
                }

                return array;
            });

            //interactions = ObjectCacheUtil.GetOrCreate(api, "idgaxeInteractions", () =>
            //{
            //    return new WorldInteraction[] {
            //        new WorldInteraction()
            //            {
            //                ActionLangCode = "indappledgroves:itemhelp-axe-chopwood",
            //                MouseButton = EnumMouseButton.Right
            //            },
            //        };
            //});
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandHandling handling)
        {
            
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);
        }

        #region TreeFelling
        public float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            
            ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
            int posx = tempAttr.GetInt("lastposX", -1);
            int posy = tempAttr.GetInt("lastposY", -1);
            int posz = tempAttr.GetInt("lastposZ", -1);
            float treeResistance = tempAttr.GetFloat("treeResistance", 1) * (itemslot.Itemstack.Collectible.Attributes["choppingProps"]["fellingMultiplier"].AsFloat(1f));


            BlockPos pos = blockSel.Position;

            if (pos.X != posx || pos.Y != posy || pos.Z != posz || counter % 30 == 0)
            {
                Stack<BlockPos> foundPositions = FindTree(player.Entity.World, pos);
                treeResistance = (float)Math.Max(1, Math.Sqrt(foundPositions.Count));

                tempAttr.SetFloat("treeResistance", treeResistance);
            }

            tempAttr.SetInt("lastposX", pos.X);
            tempAttr.SetInt("lastposY", pos.Y);
            tempAttr.SetInt("lastposZ", pos.Z);
            return treeResistance * 1.25f;

        }

        public bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player) byPlayer = byEntity.World.PlayerByUid(player.PlayerUID);

            double windspeed = api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0;
            
            Stack<BlockPos> foundPositions = FindTree(world, blockSel.Position);
            if (foundPositions.Count == 0)
            {
                return collObj.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            bool damageable = collObj.DamagedBy != null && collObj.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);

            float leavesMul = 1;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;
            while (foundPositions.Count > 0)
            {
                BlockPos pos = foundPositions.Pop();
                blocksbroken++;

                Block block = world.BlockAccessor.GetBlock(pos, 0);

                bool isLog = block.BlockMaterial == EnumBlockMaterial.Wood;
                bool isBranchy = block.Code.Path.Contains("branchy");
                bool isLeaves = block.BlockMaterial == EnumBlockMaterial.Leaves;

                world.BlockAccessor.BreakBlock(pos, byPlayer, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1));

                if (world.Side == EnumAppSide.Client)
                {
                    dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
                    dustParticles.Color |= 255 << 24;
                    dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);

                    if (block.BlockMaterial == EnumBlockMaterial.Leaves)
                    {
                        dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
                        dustParticles.ParticleModel = EnumParticleModel.Quad;
                        dustParticles.MinVelocity.Set(-0.4f + 4 * (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + 4 * (float)windspeed, 1.2f, 0.8f);

                    }
                    else
                    {
                        dustParticles.GravityEffect = 0.8f;
                        dustParticles.ParticleModel = EnumParticleModel.Cube;
                        dustParticles.MinVelocity.Set(-0.4f + (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + (float)windspeed, 1.2f, 0.8f);
                    }


                    world.SpawnParticles(dustParticles);
                }


                if (damageable && isLog)
                {
                    collObj.DamageItem(world, byEntity, itemslot);
                }

                if (itemslot.Itemstack == null) return true;

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }

            if (blocksbroken > 35)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }

            return true;
        }

        

        public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos)
        {
            Queue<Vec4i> queue = new();
            HashSet<BlockPos> checkedPositions = new();
            Stack<BlockPos> foundPositions = new();
            Block startBlock = api.World.BlockAccessor.GetBlock(startPos);
            BlockPos secondPos = null;

            api.World.BlockAccessor.WalkBlocks(startPos.AddCopy(1, 1, 1), startPos.AddCopy(-1, 1, -1), (block, x, y, z) =>
            {
                string[] woods = new[] { "log", "ferntree", "fruittree", "bamboo"};
                if (woods.Contains<string>(block.Code.FirstCodePart())) { secondPos = new BlockPos(x, y, z); }
            }, true);

            if (startBlock.Code.FirstCodePart() == "treestump")
            {
                //startPos = secondPos != null ? secondPos : startPos;
                startPos = secondPos ?? startPos;
            }

            Block block = world.BlockAccessor.GetBlock(startPos, 0);
            if (block.Code == null) return foundPositions;
            string treeFellingGroupCode = block.Attributes?["treeFellingGroupCode"].AsString();
            int spreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
            JsonObject attributes3 = block.Attributes;
            if (attributes3 != null && !attributes3["treeFellingCanChop"].AsBool(true))
            {
                return foundPositions;
            }

            // Must start with a log
            EnumTreeFellingBehavior bh = EnumTreeFellingBehavior.Chop;
            ICustomTreeFellingBehavior ctfbh = block as ICustomTreeFellingBehavior;
            if (ctfbh != null)
            {
                bh = ctfbh.GetTreeFellingBehavior(startPos, null, spreadIndex);
                if (bh == EnumTreeFellingBehavior.NoChop)
                {
                    //resistance = foundPositions.Count;
                    return foundPositions;
                }
            }
            if (spreadIndex < 2) return foundPositions;
            if (treeFellingGroupCode == null) return foundPositions;

            queue.Enqueue(new Vec4i(startPos.X, startPos.Y, startPos.Z, spreadIndex));
            foundPositions.Push(startPos);
            checkedPositions.Add(startPos);

            while (queue.Count > 0)
            {
                if (foundPositions.Count > 2500)
                {
                    break;
                }

                Vec4i pos = queue.Dequeue();
                block = world.BlockAccessor.GetBlock(pos.X, pos.Y, pos.Z);
                ICustomTreeFellingBehavior ctfbhh = block as ICustomTreeFellingBehavior;
                if (ctfbhh != null)
                {
                    bh = ctfbhh.GetTreeFellingBehavior(startPos, null, spreadIndex);
                }
                if (bh != EnumTreeFellingBehavior.NoChop)
                {
                    for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
                    {
                        Vec3i facing = Vec3i.DirectAndIndirectNeighbours[i];
                        BlockPos neibPos = new(pos.X + facing.X, pos.Y + facing.Y, pos.Z + facing.Z);

                        float hordist = GameMath.Sqrt(neibPos.HorDistanceSqTo(startPos.X, startPos.Z));
                        float vertdist = (neibPos.Y - startPos.Y);

                        // "only breaks blocks inside an upside down square base pyramid"
                        if (hordist - 1 >= 2 * vertdist) continue;
                        if (checkedPositions.Contains(neibPos)) continue;

                        block = world.BlockAccessor.GetBlock(neibPos, 0);
                        if (block.Code == null || block.Id == 0) continue;

                        string ngcode = block.Attributes?["treeFellingGroupCode"].AsString();

                        // Only break the same type tree blocks
                        if (ngcode != treeFellingGroupCode) continue;

                        // Only spread from "high to low". i.e. spread from log to leaves, but not from leaves to logs
                        int nspreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
                        if (pos.W < nspreadIndex) continue;

                        foundPositions.Push(neibPos.Copy());
                        queue.Enqueue(new Vec4i(neibPos.X, neibPos.Y, neibPos.Z, nspreadIndex));


                        checkedPositions.Add(neibPos);
                    }
                }
            }
            return foundPositions;
        }

        //Particle Handlers -> Get Particles Working
        //private SimpleParticleProperties InitializeWoodParticles()
        //{
        //    return new SimpleParticleProperties()
        //    {
        //        MinPos = new Vec3d(),
        //        AddPos = new Vec3d(),
        //        MinQuantity = 0,
        //        AddQuantity = 3,
        //        Color = ColorUtil.ToRgba(100, 200, 200, 200),
        //        GravityEffect = 1f,
        //        WithTerrainCollision = true,
        //        ParticleModel = EnumParticleModel.Quad,
        //        LifeLength = 0.5f,
        //        MinVelocity = new Vec3f(-1, 2, -1),
        //        AddVelocity = new Vec3f(2, 0, 2),
        //        MinSize = 0.07f,
        //        MaxSize = 0.1f,
        //        WindAffected = true
        //    };
        //}

        static readonly SimpleParticleProperties dustParticles = new()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            Color = ColorUtil.ToRgba(100, 200, 200, 200),
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
            WindAffected = true
        };

        #endregion TreeFelling

        //Create function by which interactions will find recipes using the target block and the current tool mode.
        public SkillItem[] toolModes;
    }
}