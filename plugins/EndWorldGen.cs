using System;
using System.Collections.Generic;

using MCGalaxy.Generator;

namespace MCGalaxy
{
	struct Pillar
	{
		public int radius;
		public int maxY;
		public bool cage;
		
		public Pillar(int radius, int maxY, bool cage)
		{
			this.radius = radius;
			this.maxY   = maxY;
			this.cage   = cage;
		}
	}
	
	public class EndWorldGen : Plugin
	{
		public override string name { get { return "EndWorldGen"; } }
		public override string MCGalaxy_Version { get { return "1.9.5.1"; } }
		public override string creator { get { return "Rainb0wSkeppy"; } }
		
		public override void Load(bool startup)
		{
            MapGen.Register("End", GenType.Advanced, generateWorld, "desc");
		}

		public override void Unload(bool shutdown)
		{
			MapGen.Generators.RemoveAll((i) => i.Theme == "End");
		}
		
        private static bool generateWorld(Player p, Level lvl, MapGenArgs args)
        {
			Pillar[] pillars = new[]{
				new Pillar(3, 76, false),
				new Pillar(3, 79, true),
				new Pillar(3, 82, true),
				new Pillar(4, 85, false),
				new Pillar(4, 88, false),
				new Pillar(4, 91, false),
				new Pillar(5, 94, false),
				new Pillar(5, 97, false),
				new Pillar(5, 100, false),
				new Pillar(6, 103, false)
			};
			
			const int cellSize = 4;
			
			const ushort endStoneBlock = 23;
			const ushort obsidianBlock = 49;
			const ushort bedrockBlock  = 7;
			const ushort cageBlock     = 44;
			const ushort fireBlock     = 54;
			
			Random random = new Random();
			ImprovedNoise perlin = new ImprovedNoise(random);
			perlin.Octaves = 1;
			
			float minY1 =  56f  / 128f * lvl.Height;
			float maxY1 =  192f / 128f * lvl.Height;
			float minY2 =  4f   / 128f * lvl.Height;
			float maxY2 =  32f  / 128f * lvl.Height;
			float minY3 =  16f  / 128f * lvl.Height;
			float maxY3 =  48f  / 128f * lvl.Height;
			
			int cellWidth  = ceil(lvl.Width  / cellSize) + 1;
			int cellHeight = ceil(lvl.Height / cellSize) + 1;
			int cellLength = ceil(lvl.Length / cellSize) + 1;
			
			int lavaHeight = ceil(lvl.Height / 4f);
			
			float[,,] densities = new float[cellWidth,cellHeight,cellLength];
			
			for (int y = 0; y < cellHeight; y++)
				for (int z = 0; z < cellLength; z++)
					for (int x = 0; x < cellWidth; x++)
			{
				float dist = (x - cellWidth / 2f) * (x - cellWidth / 2f) + (z - cellLength / 2f) * (z - cellLength / 2f);
				
				float d = (float) perlin.NormalisedNoise(
					(x * 0.25f * 80  / 2f) / 171.103f * cellSize,
					(y * 0.25f * 180 / 2f) / 171.103f * cellSize - dist / 10f,
					(z * 0.25f * 80  / 2f) / 171.103f * cellSize
				);
				
				d += map(dist, 0, (80f / cellSize) * (80f / cellSize), 0.9625f, 0);
      
				d += 23.4375f;
				d *= clamp(map(y, minY1 / cellSize, maxY1 / cellSize, 1, 0), 0, 1);
				d -= 23.4375f;
				
				d += 0.234375f;
				d *= clamp(map(y, minY2 / cellSize, maxY2 / cellSize, 0, 1), 0, 1);
				d *= clamp(map(y, minY3 / cellSize, maxY3 / cellSize, 0.5f, 1), 0.25f, 1);
				d -= 0.234375f;
				
				densities[x,y,z] = d;
			}
			
			for (int y = 0; y < lvl.Height; y++)
				for (int z = 0; z < lvl.Length; z++)
					for (int x = 0; x < lvl.Width; x++)
			{
				int cellX = x / cellSize;
				int cellY = y / cellSize;
				int cellZ = z / cellSize;
				
				float density = lerp(
					lerp(
						lerp(densities[cellX,cellY,cellZ],   densities[cellX+1,cellY,cellZ],   (x % cellSize) / (float) cellSize),
						lerp(densities[cellX,cellY+1,cellZ], densities[cellX+1,cellY+1,cellZ], (x % cellSize) / (float) cellSize),
						(y % cellSize) / (float) cellSize
					),
					lerp(
						lerp(densities[cellX,cellY,cellZ+1],   densities[cellX+1,cellY,cellZ+1],   (x % cellSize) / (float) cellSize),
						lerp(densities[cellX,cellY+1,cellZ+1], densities[cellX+1,cellY+1,cellZ+1], (x % cellSize) / (float) cellSize),
						(y % cellSize) / (float) cellSize
					),
					(z % cellSize) / (float) cellSize
				);
				
				density *= 0.64f;
				density = clamp(density, -1, 1);
				density = density / 2 - density * density * density / 24;
				
				ushort block = 0;
				
				if (density > 0)
					block = endStoneBlock;
				
				if (block != 0)
					lvl.SetBlock((ushort) x, (ushort) y, (ushort) z, block);
			}
			
			ushort centerX = (ushort) ((lvl.Width + 1) / 2);
			ushort centerZ = (ushort) ((lvl.Length + 1) / 2);
			
			int spawnX = centerX;
			int spawnY = 48 * lvl.Height / 128;
			int spawnZ = centerZ + 100;
			
			spawnPlatform(lvl, spawnX, spawnY, spawnZ, obsidianBlock);
			
			lvl.spawnx = (ushort) spawnX;
			lvl.spawny = (ushort) (spawnY + 2);
			lvl.spawnz = (ushort) spawnZ;
			
			for (int i = 0; i < pillars.Length; i++)
			{
				int j = random.Next(0, pillars.Length - 1);
				Pillar t = pillars[i];
				pillars[i] = pillars[j];
				pillars[j] = t;
			}
			
			for (int i = 0; i < pillars.Length; i++)
			{
				float a = MathF.PI * 2f / (float) pillars.Length * i;
				int x = round(centerX + MathF.Sin(a) * 43);
				int z = round(centerZ + MathF.Cos(a) * 43);
				
				Pillar p2 = pillars[i];
			
				pillar(lvl, x, p2.maxY, z, p2.radius, p2.cage, obsidianBlock, bedrockBlock, cageBlock, fireBlock);
			}
			
			int exitY = lvl.Height - 1;
			
			while (exitY >= 0 && (lvl.GetBlock((ushort) centerX, (ushort) exitY, (ushort) centerZ) == 0))
				exitY--;
			
			exitPortal(lvl, centerX, exitY, centerZ, bedrockBlock, fireBlock);
			
			lvl.Config.EdgeBlock = 0;
			lvl.Config.HorizonBlock = 0;
			lvl.Config.FogColor = "#2b082c";
			lvl.Config.SkyColor = "#000000";
			lvl.Config.LightColor = "#a0a0a0";
			lvl.Config.ShadowColor = "#606060";
			lvl.Config.LavaLightColor = "#ffd0d0";
			lvl.Config.ExpFog = 1;
			lvl.Config.MaxFogDistance = 256;
			lvl.Config.CloudsHeight = -32768;
			
            return true;
        }
		
		private static void spawnPlatform(Level lvl, int spawnX, int spawnY, int spawnZ, ushort obsidianBlock)
		{
			for (int x = spawnX - 2; x <= spawnX + 2; x++)
				for (int z = spawnZ - 2; z <= spawnZ + 2; z++)
			{
				lvl.SetBlock((ushort) x, (ushort) spawnY, (ushort) z, obsidianBlock);
				lvl.SetBlock((ushort) x, (ushort) (spawnY + 1), (ushort) z, 0);
				lvl.SetBlock((ushort) x, (ushort) (spawnY + 2), (ushort) z, 0);
				lvl.SetBlock((ushort) x, (ushort) (spawnY + 3), (ushort) z, 0);
			}
		}
		
		private static void pillar(Level lvl, int centerX, int maxY, int centerZ, int radius, bool cage, ushort obsidianBlock, ushort bedrockBlock, ushort cageBlock, ushort fireBlock)
		{
			int minX = centerX - radius + 1;
			int maxX = centerX + radius - 1;
			int minZ = centerZ - radius + 1;
			int maxZ = centerZ + radius - 1;
			
			for (int x = minX; x <= maxX; x++)
				for (int z = minZ; z <= maxZ; z++)
			{
				if (abs(x - centerX) + abs(z - centerZ) <= radius)
					for (int y = 0; y < maxY; y++)
						lvl.SetBlock((ushort) x, (ushort) y, (ushort) z, obsidianBlock);
				
				if (cage)
				{
					if (x == minX || x == maxX || z == minZ || z == maxZ)
						for (int y = maxY; y < maxY + 3; y++)
							lvl.SetBlock((ushort) x, (ushort) y, (ushort) z, cageBlock);
					
					lvl.SetBlock((ushort) x, (ushort) (maxY + 3), (ushort) z, cageBlock);
				}
			}
			
			lvl.SetBlock((ushort) centerX, (ushort) maxY, (ushort) centerZ, bedrockBlock);
			lvl.SetBlock((ushort) centerX, (ushort) (maxY + 1), (ushort) centerZ, fireBlock);
		}
		
		private static void exitPortal(Level lvl, int centerX, int exitY, int centerZ, ushort bedrockBlock, ushort fireBlock)
		{
			for (int x = centerX - 2; x <= centerX + 2; x++)
				for (int z = centerZ - 2; z <= centerZ + 2; z++)
					lvl.SetBlock((ushort) x, (ushort) exitY, (ushort) z, 0);
			
			for (int x = centerX - 3; x <= centerX + 3; x++)
				for (int z = centerZ - 3; z <= centerZ + 3; z++)
					for (int y = exitY + 1; y <= exitY + 11; y++)
						lvl.SetBlock((ushort) x, (ushort) y, (ushort) z, 0);
			
			for (int x = centerX - 2; x <= centerX + 2; x++)
				for (int z = centerZ - 2; z <= centerZ + 2; z++)
					lvl.SetBlock((ushort) x, (ushort) (exitY), (ushort) z, bedrockBlock);
			
			lvl.SetBlock((ushort) (centerX - 2), (ushort) (exitY + 1), (ushort) (centerZ - 2), bedrockBlock);
			lvl.SetBlock((ushort) (centerX + 2), (ushort) (exitY + 1), (ushort) (centerZ - 2), bedrockBlock);
			lvl.SetBlock((ushort) (centerX - 2), (ushort) (exitY + 1), (ushort) (centerZ + 2), bedrockBlock);
			lvl.SetBlock((ushort) (centerX + 2), (ushort) (exitY + 1), (ushort) (centerZ + 2), bedrockBlock);
			
			for (int y = exitY; y <= exitY + 1; y++)
				for (int i = -1; i <= 1; i++)
			{
				lvl.SetBlock((ushort) (centerX - 3), (ushort) y, (ushort) (centerZ + i), bedrockBlock);
				lvl.SetBlock((ushort) (centerX + 3), (ushort) y, (ushort) (centerZ + i), bedrockBlock);
				lvl.SetBlock((ushort) (centerX + i), (ushort) y, (ushort) (centerZ - 3), bedrockBlock);
				lvl.SetBlock((ushort) (centerX + i), (ushort) y, (ushort) (centerZ + 3), bedrockBlock);
			}
			
			for (int y = exitY; y <= exitY + 4; y++)
				lvl.SetBlock((ushort) centerX, (ushort) y, (ushort) centerZ, bedrockBlock);
			
			lvl.SetBlock((ushort) centerX, (ushort) (exitY + 5), (ushort) centerZ, fireBlock);
		}
		
		private static float sqrt(float x)
		{
			return (float) Math.Sqrt(x);
		}
		
		private static float clamp(float x, float min, float max)
		{
			if (x < min)
				return min;
			
			if (x > max)
				return max;
			
			return x;
		}
		
		private static float map(float x, float fromMin, float fromMax, float toMin, float toMax)
		{
			return (x - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
		}
		
		private static float lerp(float min, float max, float x)
		{
			return x * (max - min) + min;
		}
		
		private static int ceil(float x)
		{
			return (int) Math.Ceiling((double) x);
		}
		
		private static int round(float x)
		{
			return (int) Math.Floor((double) (x + 0.5));
		}
		
		private static int abs(int x)
		{
			return x < 0 ? -x : x;
		}
		
		private static int max(int a, int b)
		{
			return a > b ? a : b;
		}
		
		private static float maxf(float a, float b)
		{
			return a > b ? a : b;
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