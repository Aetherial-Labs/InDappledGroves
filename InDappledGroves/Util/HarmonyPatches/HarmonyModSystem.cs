﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

using HarmonyLib;
using Vintagestory.ServerMods.NoObf;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using InDappledGroves.Util.WorldGen;
using InDappledGroves.Util.Config;
using Vintagestory.GameContent;
using Vintagestory.API.Util;

namespace InDappledGroves.Util.HarmonyPatches
{
    public class HarmonyModSystem : ModSystem
    {
        private Harmony harmony;
        private readonly string harmonyId = "teacupangel.vinternacht.transpiler";

        private static ICoreServerAPI sapi;
        private static IWorldGenBlockAccessor thisBlockAccessor;
        private static bool isWideTrunk;

        private static Dictionary<BlockPos, Block> TreeBase = new();

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            harmony = new Harmony(harmonyId);

            sapi.Logger.Notification("VinterTranspiler: Executing");
            TreeGenPatch.Execute(harmony);
        }

        public static void GrowBranchTranspilerCall(IWorldGenBlockAccessor blockAccessor, int depth, int iteration, int blockId, BlockPos currentPos, bool wideTrunk)
        {
            //grab the new blockaccessor
            thisBlockAccessor = blockAccessor;
            if (depth == 0 && ((!wideTrunk && iteration == 1) || (wideTrunk && TreeBase.Count < 4)))
            {
                if (((wideTrunk && TreeBase.Count < 4 || !wideTrunk && TreeBase.Count < 1)))
                {
                    Block block = blockAccessor.GetBlock(blockId);
                    if (block.FirstCodePart() == "log")
                    {
                        TreeBase[currentPos.Copy()] = block;
                    }

                    if (block.FirstCodePart() == "logsection" && (TreeBase.Count == 0 || currentPos.Y == TreeBase.First().Key.Y))
                    {
                        TreeBase[currentPos.Copy()] = block;
                    }

                    if (block.FirstCodePart() == "sapling" && (TreeBase.Count == 0 || currentPos.Y == TreeBase.First().Key.Y))
                    {
                        TreeBase[currentPos.Copy()] = block;
                    }
                }
            }
            wideTrunk = isWideTrunk;
        }

        public static void growTreePostfix()
        {
            if (TreeBase.Count > 0 )
            {
                foreach (KeyValuePair<BlockPos, Block> entry in TreeBase)
                {
                    TreeHollows.TreeDone.OnTreeGenComplete(TreeBase, thisBlockAccessor, isWideTrunk = false);
                }
                TreeBase.Clear();
                isWideTrunk = false;
            }
            

            
        }

        //public static void checkGrowPostfix()
        //{
        //    if (TreeBase.Count > 0)
        //    {
        //        foreach (KeyValuePair<BlockPos, Block> entry in TreeBase)
        //        {
        //            TreeHollows.TreeDone.OnTreeGenComplete(TreeBase, thisBlockAccessor, isWideTrunk = false);
        //        }
        //        TreeBase.Clear();
        //        isWideTrunk = false;
        //    }
        //}

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            harmony = null;
            isWideTrunk = false;
            sapi = null;
            thisBlockAccessor = null;
            TreeBase.Clear();

        }
    }
}
