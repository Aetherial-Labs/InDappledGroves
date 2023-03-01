using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.Items
{
	class IDGFirewood : ItemFirewood
	{

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			if (blockSel == null) return;
			BlockPos position = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(position, 0);

			//Determine if the targeted block is any of the three blocks indicated
			if (block is BlockFirepit || block is BlockPitkiln || block is BlockClayOven)
			{
				return;
			}

			//Determine if target block is *not* a BlockFirewoodPile
			if (!byEntity.Controls.Sneak && !byEntity.Controls.Sprint && block is not BlockFirewoodPile)
			{

				string failurecode = "";

				Block targetBlock = api.World.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockSel.Face), 0);
				ItemStack stackWood = new(api.World.BlockAccessor.GetBlock(new AssetLocation("indappledgroves:idgblockfirewood")));
				bool flag = false;
				if (targetBlock.Replaceable > 5000 && stackWood.Block.TryPlaceBlock(byEntity.World, ((EntityPlayer)byEntity).Player, stackWood, blockSel, ref failurecode))
				{
					flag = true;
					slot.TakeOut(1);
				}
				else
				{
					BlockSelection bs2 = blockSel;
					bs2.Position = bs2.Position.AddCopy(blockSel.Face);
					if (stackWood.Block.TryPlaceBlock(byEntity.World, ((EntityPlayer)byEntity).Player, stackWood, bs2, ref failurecode))
					{
						flag = true;
						slot.TakeOut(1);
					}
				}
				if (flag)
				{
					this.api.World.PlaySoundAt(new AssetLocation("sounds/player/build"), byEntity, ((EntityPlayer)byEntity).Player, true, 16f, 1f);
				}
			}
			handling = EnumHandHandling.PreventDefault;
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}
}