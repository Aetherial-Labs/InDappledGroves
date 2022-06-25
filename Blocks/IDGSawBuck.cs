using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGSawBuck : Block
	{
		// Token: 0x06000BD6 RID: 3030 RVA: 0x000068EB File Offset: 0x00004AEB
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack chopToolStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject chopCollObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBESawBuck besawbuck) return base.OnBlockInteractStart(world, byPlayer, blockSel);
			if (chopCollObj != null && chopCollObj.HasBehavior<BehaviorWoodSawer>()
				&& !besawbuck.Inventory.Empty)
			{
				if (chopToolStack.Attributes.GetInt("durability") < chopCollObj.GetBehavior<BehaviorWoodSawer>().sawBuckSawDamage)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", chopCollObj.GetBehavior<BehaviorWoodSawer>().sawBuckSawDamage));
					return base.OnBlockInteractStart(world, byPlayer, blockSel);
				}

				byPlayer.Entity.StartAnimation("axechop");
				return true;
			}
			return besawbuck.OnInteract(byPlayer);
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			CollectibleObject chopTool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			IDGBESawBuck bebesawbuck = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck;
			BlockPos pos = blockSel.Position;

			if (chopTool != null && chopTool.HasBehavior<BehaviorWoodSawer>() && !bebesawbuck.Inventory.Empty)
			{
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
				}
				if (secondsUsed >= chopTool.GetBehavior<BehaviorWoodSawer>().sawBuckSawTime)
				{
					chopTool.GetBehavior<BehaviorWoodSawer>().SpawnOutput(bebesawbuck.Inventory[0].Itemstack.Collectible,
						byPlayer.Entity, blockSel.Position, chopTool.GetBehavior<BehaviorWoodSawer>().sawBuckSawDamage);

					bebesawbuck.Inventory.Clear();
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck).updateMeshes();
					bebesawbuck.MarkDirty(true);
				}
				return !bebesawbuck.Inventory.Empty;
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
