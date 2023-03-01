using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
	class IDGBESawBuck : BlockEntityDisplay
	{
		public override InventoryBase Inventory { get; }
		public override string InventoryClassName => "sawbuck";
		public override string AttributeTransformCode => "onDisplayTransform";

		public IDGBESawBuck()
		{
			Inventory = new InventoryGeneric(2, "sawbuck-slot", null, null);
		}

		public ItemSlot InputSlot
		{
			get { return Inventory[0]; }
		}

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			this.capi = (api as ICoreClientAPI);
			if (this.capi != null)
			{
				this.updateMeshes();
			}
		}

		internal bool OnInteract(IPlayer byPlayer)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

			if (activeHotbarSlot.Empty)
			{
				return this.TryTake(byPlayer);
			}

			if (!activeHotbarSlot.Empty && !Inventory.Empty) return true;

			//Get Collectible Object and Attributes from the Collectible Object
			//Then check to see if attributes is null, or if chopblock is false or absent

			ItemStack itemstack = activeHotbarSlot.Itemstack;
			AssetLocation assetLocation;
			if (itemstack == null)
			{
				assetLocation = null;
			}
			else
			{
				Block block = itemstack.Block;
				if (block == null)
				{
					assetLocation = null;
				}
				else
				{
					BlockSounds sounds = block.Sounds;
					assetLocation = (sounds?.Place);
				}
			}
			AssetLocation assetLocation2 = assetLocation;
			if (DoesSlotMatchRecipe(activeHotbarSlot) &&  this.TryPut(activeHotbarSlot))
			{
				this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
				updateMeshes();
				return true;
			}
			return false;
		}

		private bool TryPut(ItemSlot slot)
		{
			for (int i = 0; i < Inventory.Count; i++)
			{
				if (this.Inventory[i].Empty)
				{
					int num3 = slot.TryPutInto(this.Api.World, this.Inventory[i], 1);
					this.updateMeshes();
					base.MarkDirty(true, null);
					return num3 > 0;
				}
			}
			return false;
		}

		private bool TryTake(IPlayer byPlayer)
		{
			for (int i = 0; i < Inventory.Count; i++)
			{
				if (!this.Inventory[i].Empty)
				{
					ItemStack itemStack = this.Inventory[i].TakeOut(1);
					if (byPlayer.InventoryManager.TryGiveItemstack(itemStack, false))
					{
						Block block = itemStack.Block;
						AssetLocation assetLocation;
						if (block == null)
						{
							assetLocation = null;
						}
						else
						{
							BlockSounds sounds = block.Sounds;
							assetLocation = (sounds?.Place);
						}
						AssetLocation assetLocation2 = assetLocation;
						this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
					}
					if (itemStack.StackSize > 0)
					{
						this.Api.World.SpawnItemEntity(itemStack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
					}
					base.MarkDirty(true, null);
					this.updateMeshes();
					return true;
				}
			}
			return false;
		}

		public void SpawnOutput(SawbuckRecipe recipe, BlockPos pos)
		{
			int j = recipe.Output.StackSize;
			for (int i = j; i > 0; i--)
			{
				Api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
			}

		}

		public bool DoesSlotMatchRecipe(ItemSlot slots)
		{
			List<SawbuckRecipe> recipes = IDGRecipeRegistry.Loaded.SawbuckRecipes;
			if (recipes == null) return false;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots))
				{
					return true;
				}
			}

			return false;
		}

		public SawbuckRecipe GetMatchingSawbuckRecipe(ItemSlot slots, string toolmode)
		{
			List<SawbuckRecipe> recipes = IDGRecipeRegistry.Loaded.SawbuckRecipes;
			if (recipes == null) return null;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots) && (recipes[j].ToolMode == toolmode))
				{
					return recipes[j];
				}
			}

			return null;
		}

		protected ModelTransform genTransform(ItemStack stack)
		{
			String side = Block.Variant["side"];
			ModelTransform transform;

			if (stack != null && stack.Collectible.Attributes["workStationTransforms"]["idgSawBuckProps"]["idgSawBuckTransform"].Exists)
			{
				transform = ProcessTransform(stack.Collectible.Attributes["workStationTransforms"]["idgSawBuckProps"]["idgSawBuckTransform"].AsObject<ModelTransform>(), side);
			}
			else 
			{
				transform = new ModelTransform
				{
					Translation = new Vec3f(),
					Rotation = new Vec3f(),
					Origin = new Vec3f()
				};
			}

			transform.EnsureDefaultValues();
			return transform;
		}

		private ModelTransform ProcessTransform(ModelTransform transform, String side)
		{
			transform.Rotation.X += AddRotate(side, "x");
			transform.Rotation.Y = (Block.Shape.rotateY) + AddRotate(side, "y");
			transform.Rotation.Z += AddRotate(side, "z");
			transform.Translation.X += AddTranslate(side, "x");
			transform.Translation.Y +=  AddTranslate(side, "y");
			transform.Translation.Z += AddTranslate(side, "z");
			transform.Origin.X += AddOrigin(side, "x");
			transform.Origin.Y += AddOrigin(side, "y");
			transform.Origin.Z += AddOrigin(side, "z");
			transform.Scale = AddScale(side);
			return transform;
		}

		public float AddRotate(string side, string axis)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawBuckProps"]["idgSawBuckTransform"];
			return transforms["rotation"][side+axis].Exists ? transforms["rotation"][side+axis].AsFloat() : 0f;
		}

		public float AddTranslate(string side, string axis)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawBuckProps"]["idgSawBuckTransform"];
			return transforms["translation"][side+axis].Exists ? transforms["translation"][side+axis].AsFloat() : 0f;
		}

		public float AddOrigin(string side, string axis)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawBuckProps"]["idgSawBuckTransform"];
			return transforms["origin"][side+axis].Exists ? transforms["origin"][side+axis].AsFloat() : 0f;
		}

		public float AddScale(string side)
		{
			JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["workStationTransforms"]["idgSawBuckProps"]["idgSawBuckTransform"];
			return transforms["scale"][side].Exists ? transforms["scale"][side].AsFloat() : 1f;
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			//Alter this code to produce an output based on the recipe that results from the held tool and its current mode.
			//If no tool is held, return only contents
		}

		protected override float[][] genTransformationMatrices()
		{
			float[][] tfMatrices = new float[1][];
			for (int index = 0; index < 1; index++)
			{
					ItemStack itemstack = this.Inventory[index].Itemstack;
					if (itemstack != null)
					{
						tfMatrices[index] = new Matrixf().Set(genTransform(itemstack).AsMatrix).Values;
					}
			}
			return tfMatrices;
		}

	}
}