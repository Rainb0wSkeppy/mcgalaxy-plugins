using System;
using System.Collections.Generic;

using MCGalaxy;
using MCGalaxy.Generator;
using LibNoise;

namespace MCGalaxy
{
	public class NetherWorldGen : Plugin
	{
		public override string name { get { return "NetherWorldGen"; } }
		public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
		public override string creator { get { return "Rainb0wSkeppy"; } }
		
		public override void Load(bool startup)
		{
			MapGen.Register("Nether", GenType.Advanced, generateWorld, "desc");
		}

		public override void Unload(bool shutdown)
		{
			MapGen.Generators.RemoveAll((i) => i.Theme == "Nether");
		}
		
		private static bool generateWorld(Player p, Level lvl, MapGenArgs args)
		{
			const int cellSize = 4;
			
			const int bedrockSize = 5;
			const bool sideBedrock = true;
			
			const ushort lavaBlock       = Block.StillLava;
			const ushort netherrackBlock = Block.Red;
			const ushort bedrockBlock    = Block.Bedrock;
			const ushort magmaBlock      = Block.MagmaBlock;
			const ushort glowstoneBlock  = Block.Yellow;
			
			Random rng = new Random();
			Perlin perlin = new Perlin();
			
			double minY1 =  104.0 / 128.0 * lvl.Height;
			double maxY1 =  128.0 / 128.0 * lvl.Height;
			double minY2 = -8.0   / 128.0 * lvl.Height;
			double maxY2 =  24.0  / 128.0 * lvl.Height;
			
			int cellWidth  = ceil(lvl.Width  / cellSize) + 1;
			int cellHeight = ceil(lvl.Height / cellSize) + 1;
			int cellLength = ceil(lvl.Length / cellSize) + 1;
			
			int lavaHeight = ceil(lvl.Height / 4f);
			
			double[,,] densities = new double[cellWidth,cellHeight,cellLength];
			
			for (int y = 0; y < cellHeight; y++)
				for (int z = 0; z < cellLength; z++)
					for (int x = 0; x < cellWidth; x++)
			{
				double d = perlin.GetValue(
					(x * 0.25f  * 80  / 4f) / 171.103f * cellSize,
					(y * 0.375f * 120 / 4f) / 171.103f * cellSize,
					(z * 0.25f  * 80  / 4f) / 171.103f * cellSize
				);
				
				d = d * 2 - 1;
				
				d -= 0.9375;
				d *= clamp(map(y, minY1 / cellSize, maxY1 / cellSize, 1, 0), 0, 1);
				d += 0.9375;
				
				d -= 2.5;
				d *= clamp(map(y, minY2 / cellSize, maxY2 / cellSize, 0, 1), 0, 1);
				d += 2.5;
				
				densities[x,y,z] = d;
			}
			
			for (int y = 0; y < lvl.Height; y++)
				for (int z = 0; z < lvl.Length; z++)
					for (int x = 0; x < lvl.Width; x++)
			{
				int cellX = x / cellSize;
				int cellY = y / cellSize;
				int cellZ = z / cellSize;
				
				double density = lerp(
					lerp(
						lerp(densities[cellX,cellY,cellZ],   densities[cellX+1,cellY,cellZ],   (x % cellSize) / (double) cellSize),
						lerp(densities[cellX,cellY+1,cellZ], densities[cellX+1,cellY+1,cellZ], (x % cellSize) / (double) cellSize),
						(y % cellSize) / (double) cellSize
					),
					lerp(
						lerp(densities[cellX,cellY,cellZ+1],   densities[cellX+1,cellY,cellZ+1],   (x % cellSize) / (double) cellSize),
						lerp(densities[cellX,cellY+1,cellZ+1], densities[cellX+1,cellY+1,cellZ+1], (x % cellSize) / (double) cellSize),
						(y % cellSize) / (double) cellSize
					),
					(z % cellSize) / (double) cellSize
				);
				
				density *= 0.64f;
				density = clamp(density, -1, 1);
				density = density / 2 - density * density * density / 24;
				
				ushort block = 0;
				
				if (density > 0)
					block = netherrackBlock;
				
				int bedrockChance = 0;
				
				if (y < bedrockSize)
					bedrockChance = (int) map(y, 0, bedrockSize, 255, 0);
				
				if (y > lvl.Height - bedrockSize - 1)
					bedrockChance = (int) map(y, lvl.Height - 1, lvl.Height - bedrockSize - 1, 255, 0);
				
				if (sideBedrock)
				{
					if (x < bedrockSize)
						bedrockChance = Math.Max(bedrockChance, (int) map(x, 0, bedrockSize, 255, 0));
					
					if (x > lvl.Width - bedrockSize - 1)
						bedrockChance = Math.Max(bedrockChance, (int) map(x, lvl.Width - 1, lvl.Width - bedrockSize - 1, 255, 0));
					
					if (z < bedrockSize)
						bedrockChance = Math.Max(bedrockChance, (int) map(z, 0, bedrockSize, 255, 0));
					
					if (z > lvl.Length - bedrockSize - 1)
						bedrockChance = Math.Max(bedrockChance, (int) map(z, lvl.Length - 1, lvl.Length - bedrockSize - 1, 255, 0));
				}
				
				if (bedrockChance > 0)
					if ((hash((uint) (x ^ (z << 6) ^ (z >> 6) ^ (y << 12) ^ (y >> 12))) & 0xff) <= bedrockChance)
						block = bedrockBlock;
				
				if (block == 0 && y < lavaHeight)
					block = lavaBlock;
				
				if (block != 0)
					lvl.SetBlock((ushort) x, (ushort) y, (ushort) z, block);
			}
			
			for (int y = 0; y < lvl.Height; y += 16)
				for (int z = 0; z < lvl.Length; z += 16)
					for (int x = 0; x < lvl.Width; x += 16)
						for (int i = 0; i < 4; i++)
			{
				int lx = x + rng.Next(0, 16);
				int ly = y + rng.Next(0, 16);
				int lz = z + rng.Next(0, 16);
				
				if (lvl.GetBlock((ushort) lx, (ushort) ly, (ushort) lz) != 0)
					lava(lvl, lx, ly, lz, bedrockBlock, lavaBlock, 0);
			}
			
			int magmaMinY = (27 * lvl.Height) / 128;
			int magmaMaxY = (37 * lvl.Height) / 128;
			
			for (int z = 0; z < lvl.Length; z += 16)
				for (int x = 0; x < lvl.Width; x += 16)
					for (int i = 0; i < 4; i++)
			{
				int vx = x + rng.Next(0, 16);
				int vy = rng.Next(magmaMinY, magmaMaxY);
				int vz = z + rng.Next(0, 16);
				
				magma(lvl, rng, vx, vy, vz, magmaBlock, netherrackBlock);
			}
			
			for (int z = 0; z < lvl.Length; z += 32)
				for (int x = 0; x < lvl.Width; x += 32)
			{
				int gx = x + rng.Next(0, 32);
				int gz = z + rng.Next(0, 32);
				int y = lvl.Height - 1;
				
				for (; y > 0; y--)
				{
					ushort b = lvl.GetBlock((ushort) gx, (ushort) y, (ushort) gz);
					
					if (b != 7 && b != netherrackBlock)
						break;
				}
				
				if (lvl.GetBlock((ushort) gx, (ushort) y, (ushort) gz) == 0)
					glowstone(lvl, rng, (ushort) gx, y, (ushort) gz, glowstoneBlock);
			}
			
			lvl.Config.EdgeLevel = lavaHeight;
			lvl.Config.SidesOffset = 5 - lavaHeight;
			lvl.Config.EdgeBlock = 7;
			lvl.Config.HorizonBlock = 10;
			lvl.Config.FogColor = "#0d0202";
			lvl.Config.SkyColor = "#0d0202";
			lvl.Config.ShadowColor = "#404040";
			lvl.Config.LavaLightColor = "#ffd0d0";
			lvl.Config.ExpFog = 1;
			lvl.Config.MaxFogDistance = 96;
			lvl.Config.CloudsHeight = -1;
			
			return true;
		}
		
		private static void lava(Level lvl, int x, int y, int z, ushort bedrockBlock, ushort lavaBlock, int h)
		{
			ushort b = lvl.GetBlock((ushort) x, (ushort) y, (ushort) z);
			
			if (b == bedrockBlock || b == lavaBlock)
				return;
			
			lvl.SetBlock((ushort) x, (ushort) y, (ushort) z, lavaBlock);
			
			bool down = lvl.GetBlock((ushort) x, (ushort) (y - 1), (ushort) z) == 0;
			
			if (down)
			{
				lava(lvl, x, y - 1, z, bedrockBlock, lavaBlock, 0);
				return;
			}
			
			if (lvl.GetBlock((ushort) x, (ushort) (y - 1), (ushort) z) == lavaBlock)
				return;
			
			if (h > 4)
				return;
			
			bool n = lvl.GetBlock((ushort) x,       (ushort) y, (ushort) (z - 1)) == 0;
			bool e = lvl.GetBlock((ushort) (x + 1), (ushort) y, (ushort) z)       == 0;
			bool s = lvl.GetBlock((ushort) x,       (ushort) y, (ushort) (z + 1)) == 0;
			bool w = lvl.GetBlock((ushort) (x - 1), (ushort) y, (ushort) z)       == 0;
			
			bool nd = lvl.GetBlock((ushort) x,       (ushort) (y - 1), (ushort) (z - 1)) == 0 && n;
			bool ed = lvl.GetBlock((ushort) (x + 1), (ushort) (y - 1), (ushort) z)       == 0 && e;
			bool sd = lvl.GetBlock((ushort) x,       (ushort) (y - 1), (ushort) (z + 1)) == 0 && s;
			bool wd = lvl.GetBlock((ushort) (x - 1), (ushort) (y - 1), (ushort) z)       == 0 && w;
			
			if (nd || ed || sd || wd)
			{
				if (nd)
				{
					lvl.SetBlock((ushort) x, (ushort) y, (ushort) (z - 1), lavaBlock);
					lava(lvl, x, y - 1, z - 1, bedrockBlock, lavaBlock, 0);
				}
				
				if (ed)
				{
					lvl.SetBlock((ushort) (x + 1), (ushort) y, (ushort) z, lavaBlock);
					lava(lvl, x + 1, y - 1, z, bedrockBlock, lavaBlock, 0);
				}
				
				if (sd)
				{
					lvl.SetBlock((ushort) x, (ushort) y, (ushort) (z + 1), lavaBlock);
					lava(lvl, x, y - 1, z + 1, bedrockBlock, lavaBlock, 0);
				}
				
				if (wd)
				{
					lvl.SetBlock((ushort) (x - 1), (ushort) y, (ushort) z, lavaBlock);
					lava(lvl, x - 1, y - 1, z, bedrockBlock, lavaBlock, 0);
				}
				
				return;
			}
			
			if (n)
				lava(lvl, x, y, z - 1, bedrockBlock, lavaBlock, h + 1);
			
			if (e)
				lava(lvl, x + 1, y, z, bedrockBlock, lavaBlock, h + 1);
			
			if (n)
				lava(lvl, x, y, z + 1, bedrockBlock, lavaBlock, h + 1);
			
			if (e)
				lava(lvl, x - 1, y, z, bedrockBlock, lavaBlock, h + 1);
		}
		
		private static void magma(Level lvl, Random rng, int x, int y, int z, ushort magmaBlock, ushort netherrackBlock)
		{
			double dz = (rng.NextDouble() * 2.0 - 1.0);
			double m = Math.Sqrt(1 - dz * dz);
			double th = (rng.NextDouble() * Math.PI * 2.0 - Math.PI);
			double dx = m * Math.Sin(th);
			double dy = m * Math.Cos(th);
			
			for (int i = 0; i < 7; i++)
			{
				int sx = (int) lerp(dx * -3, dx * 3, i / 6.0) + x;
				int sy = (int) lerp(dy * -3, dy * 3, i / 6.0) + y;
				int sz = (int) lerp(dz * -3, dz * 3, i / 6.0) + z;
				
				for (int j = 0; j < 3; j++)
					for (int k = 0; k < 3; k++)
						for (int l = 0; l < 3; l++)
							if (j == 1 || j == 2 || k == 1 || k == 2 || l == 1 || l == 2)
				{
					ushort bx = (ushort) (sx + j);
					ushort by = (ushort) (sy + k);
					ushort bz = (ushort) (sz + l);
					
					if (lvl.GetBlock(bx, by, bz) == netherrackBlock)
						lvl.SetBlock(bx, by, bz, magmaBlock);
				}
			}
		}
		
		private static void glowstone(Level lvl, Random rng, int x, int y, int z, ushort glowstoneBlock)
		{
			for (int i = 0; i < rng.Next(4, 6); i++)
			{
				lvl.SetBlock((ushort) x, (ushort) (y - i), (ushort) z, glowstoneBlock);
				
				for (int j = -5; j <= 5; j++)
					for (int k = -5; k <= 5; k++)
				{
					int d = abs(j) + abs(k);
					
					if (d < rng.Next(-3, 7 - i))
					{
						ushort bx = (ushort) (x + j);
						ushort by = (ushort) (y - i);
						ushort bz = (ushort) (z + k);
						
						if (lvl.GetBlock(bx, by, bz) == 0)
							lvl.SetBlock(bx, by, bz, glowstoneBlock);
					}
				}
			}
		}
		
		private static double clamp(double x, double min, double max)
		{
			if (x < min)
				return min;
			
			if (x > max)
				return max;
			
			return x;
		}
		
		private static int abs(int x)
		{
			if (x < 0)
				return -x;
			
			return x;
		}
		
		private static double map(double x, double fromMin, double fromMax, double toMin, double toMax)
		{
			return (x - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
		}
		
		private static double lerp(double min, double max, double x)
		{
			return x * (max - min) + min;
		}
		
		private static int ceil(double x)
		{
			return (int) Math.Ceiling(x);
		}
		
		private static uint hash(uint state)
		{
			state ^= 0x27476364;
			state *= 0x26544357;
			state ^= state >> 16;
			state *= 0x65443576;
			state ^= state >> 16;
			state *= 0x54435769;
			
			return state;
		}
	}
}
