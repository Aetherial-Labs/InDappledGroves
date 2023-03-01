using System;
using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Interfaces;
using InDappledGroves.Util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{

	internal class IDGSawHorse : Block
	{
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}


		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
		{
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}


		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}


		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
		{
            bool flag = api.World.BlockAccessor.GetBlockEntity(pos) is not IDGBESawHorse;
            bool flag2 = api.World.BlockAccessor.GetBlockEntity(neibpos) is not IDGBESawHorse;
			//string text2 = this.api.World.BlockAccessor.GetBlock(pos).Variant["side"];
			if (flag && flag2)
			{
				base.OnNeighbourBlockChange(world, pos, neibpos);
			}
			if (!flag && !flag2)
			{
                IDGBESawHorse idgbesawHorse = (this.api.World.BlockAccessor.GetBlockEntity(pos) as IDGBESawHorse);
                IDGBESawHorse idgbesawHorse2 = (this.api.World.BlockAccessor.GetBlockEntity(neibpos) as IDGBESawHorse);
                if (!idgbesawHorse.IsPaired && !idgbesawHorse2.IsPaired && this.IsNotDiagonal(pos, neibpos))
				{
					idgbesawHorse.CreateSawhorseStation(neibpos);
					idgbesawHorse2.ConBlockPos = pos.Copy();
					idgbesawHorse2.pairedBlockPos = pos.Copy();
					idgbesawHorse2.IsPaired = true;
					idgbesawHorse2.IsConBlock = false;
					Block block = this.api.World.BlockAccessor.GetBlock(this.api.World.BlockAccessor.GetBlock(pos).CodeWithVariants(new string[]
					{
						"side",
						"state"
					}, new string[]
					{
						this.getFacing(pos, neibpos, "first"),
						"compound"
					}));
					Block block2 = this.api.World.BlockAccessor.GetBlock(this.api.World.BlockAccessor.GetBlock(pos).CodeWithVariants(new string[]
					{
						"side",
						"state"
					}, new string[]
					{
						this.getFacing(pos, neibpos, "second"),
						"compound"
					}));
					this.api.World.BlockAccessor.ExchangeBlock(block.BlockId, neibpos);
					this.api.World.BlockAccessor.ExchangeBlock(block2.BlockId, pos);
					idgbesawHorse.MarkDirty(true, null);
					idgbesawHorse2.MarkDirty(true, null);
				}
			}
			base.OnNeighbourBlockChange(world, pos, neibpos);
		}

		/// <summary>Determines whether [is not diagonal] [the specified position].</summary>
		/// <param name="pos">The position of the placed sawhorse block</param>
		/// <param name="neibpos">The position of a neighboring sawhorse block</param>
		/// <returns> <c>true</c> if [is not diagonal] [the specified position]; otherwise, <c>false</c>.</returns>
		private bool IsNotDiagonal(BlockPos pos, BlockPos neibpos)
		{
			return pos == neibpos.EastCopy(1) || pos == neibpos.WestCopy(1) || pos == neibpos.NorthCopy(1) || pos == neibpos.SouthCopy(1);
		}

		/// <summary>Determines what the appropriate placing is based on the location of the first and second blocks that make up a sawhorse station relative to each other.</summary>
		/// <param name="pos">The position of the first block in a sawhorse station</param>
		/// <param name="neibpos">The position of the second block in a sawhorse station</param>
		/// <param name="which">A string indicating which of the two blocks that make up a sawhorse station are being checked.</param>
		private string getFacing(BlockPos pos, BlockPos neibpos, string which)
		{
			if (which == "first")
			{
				if (pos == neibpos.EastCopy(1))
				{
					return "east";
				}
				if (pos == neibpos.NorthCopy(1))
				{
					return "north";
				}
				if (pos == neibpos.WestCopy(1))
				{
					return "west";
				}
				if (pos == neibpos.SouthCopy(1))
				{
					return "south";
				}
			}
			if (which == "second")
			{
				if (pos == neibpos.EastCopy(1))
				{
					return "west";
				}
				if (pos == neibpos.NorthCopy(1))
				{
					return "south";
				}
				if (pos == neibpos.WestCopy(1))
				{
					return "east";
				}
				if (pos == neibpos.SouthCopy(1))
				{
					return "north";
				}
			}
			return "south";
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
            return this.api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is IDGBESawHorse idgbesawHorse && this.api.World.BlockAccessor.GetBlock(blockSel.Position).Variant["state"] == "compound" && idgbesawHorse.OnInteract(byPlayer, blockSel);
        }

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collectibleObject = itemstack?.Collectible;
			IDGBESawHorse idgbesawHorse = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawHorse;
			IDGBESawHorse idgbesawHorse2 = idgbesawHorse.IsConBlock ? idgbesawHorse : (this.api.World.BlockAccessor.GetBlockEntity(idgbesawHorse.ConBlockPos) as IDGBESawHorse);
			BlockPos position = blockSel.Position;
			string curTMode = "";
			if (collectibleObject != null)
			{
                if (collectibleObject is IIDGTool iidgtool)
                {
                    curTMode = iidgtool.GetToolModeName(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
                    toolModeMod = iidgtool.getToolModeMod(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
                }
            }
			if (collectibleObject != null && collectibleObject.HasBehavior<BehaviorWoodPlaning>(false) && !idgbesawHorse2.Inventory.Empty)
			{
				this.recipe = idgbesawHorse2.GetMatchingSawHorseRecipe(idgbesawHorse2.InputSlot(), curTMode);
				if (this.recipe != null)
				{
					if (this.playNextSound < secondsUsed)
					{
						this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
						this.playNextSound += 0.7f;
					}

					float curMiningProgress = (secondsUsed + (curDmgFromMiningSpeed)) * (toolModeMod * InDappledGrovesConfig.Current.baseWorkstationMiningSpdMult);
					float curResistance = resistance * InDappledGrovesConfig.Current.baseWorkstationResistanceMult;
					if (curMiningProgress >= curResistance)
					{
						idgbesawHorse2.SpawnOutput(this.recipe, blockSel.Position);
						idgbesawHorse2.Inventory.Clear();
						(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawHorse).updateMeshes();
						idgbesawHorse2.MarkDirty(true, null);
					}
					return !idgbesawHorse2.Inventory.Empty;
				}
			}
			return false;
		}

		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			resistance = 0;
			curDmgFromMiningSpeed = 0;
			this.playNextSound = 0.7f;
			byPlayer.Entity.StopAnimation("axechop");
		}

		public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
		{
            if (api.World.BlockAccessor.GetBlockEntity(pos) is IDGBESawHorse)
            {
                IDGBESawHorse idgbesawHorse = (this.api.World.BlockAccessor.GetBlockEntity(pos) as IDGBESawHorse);
                if (idgbesawHorse.IsPaired)
                {
                    if (this.api.World.BlockAccessor.GetBlockEntity(idgbesawHorse.pairedBlockPos) is IDGBESawHorse idgbesawHorse2)
                    {
                        this.api.World.BlockAccessor.ExchangeBlock(this.api.World.BlockAccessor.GetBlock(this.api.World.BlockAccessor.GetBlock(idgbesawHorse2.Pos).CodeWithVariant("state", "single")).BlockId, idgbesawHorse2.Pos);
                        idgbesawHorse2.IsConBlock = false;
                        idgbesawHorse2.ConBlockPos = null;
                        idgbesawHorse2.IsPaired = false;
                        idgbesawHorse2.pairedBlockPos = null;
                        if (!idgbesawHorse2.Inventory[1].Empty)
                        {
                            this.api.World.SpawnItemEntity(idgbesawHorse2.Inventory[1].TakeOutWhole(), pos.ToVec3d(), new Vec3d(0.0, 0.15000000596046448, 0.0));
                        }
                        idgbesawHorse2.MarkDirty(true, null);
                    }
                }
            }
            base.OnBlockRemoved(world, pos);
		}

		private IDGRecipeNames.SawHorseRecipe recipe;
		private float toolModeMod;
		private float playNextSound;
		private float resistance;
		private float curDmgFromMiningSpeed;
	}
}
