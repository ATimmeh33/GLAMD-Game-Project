using UnityEngine;
using System;
using System.Linq;
using Assets.Scripts.Extensions;
using Assets.Scripts.Models;
using Assets.Scripts.Helpers;
using Assets.Scripts.Models.Tiles;

namespace Assets.Scripts.Utilities
{
	public class TilePopulator
		: MonoBehaviour
	{
		private class TilePopulation
		{
			public TilePopulation(StraightTile tile)
			{
				Tile = tile;
				Blocks = new SpawnObject[5][][,];
			}

			public StraightTile Tile { get; set; }

			// Blocks of gameplay
			public SpawnObject[][][,] Blocks { get; set; }

			// SpawnPositions
			public SpawnObject[,] SpawnObstacles { get; set; }
			public SpawnObject[,] GroundSpawns { get; set; }
			public SpawnObject[,] JumpOverObstacles { get; set; }
			public SpawnObject[,] AirSpawns { get; set; }
			public SpawnObject[,] BoxCoins { get; set; }
			public SpawnObject[,] Boxes { get; set; }
			public SpawnObject MoveTrigger { get; set; }
		}

		// Obstacles
		[SerializeField]
		private GameObject container;
		[SerializeField]
		private GameObject cartoonCar;
		[SerializeField]
		private GameObject jeep;
		[SerializeField]
		private GameObject box;
		[SerializeField]
		private GameObject hotdogTruck;
		[SerializeField]
		private GameObject pizzaTruck;
		[SerializeField]
		private GameObject cone;
		[SerializeField]
		private GameObject bin;
		[SerializeField]
		private GameObject roadBarrier;
		[SerializeField]
		private GameObject singleBarrier;
		[SerializeField]
		private GameObject alternativeBox;

		// Pickups
		[SerializeField]
		public GameObject doubleCoins;
		[SerializeField]
		public GameObject inhaler;
		[SerializeField]
		public GameObject slowmotion;
		[SerializeField]
		public GameObject heart;
		[SerializeField]
		public int maxPowerup; // max aantal powerups per straightile


		// Coins
		[SerializeField]
		public GameObject coin;
		[SerializeField]
		public GameObject diamond;

		// MoveTrigger
		[SerializeField]
		public GameObject moveTrigger;

		// Arraynumbers
		private int spawnobstacles = 0;
		private int airspawns = 1;
		private int boxes = 2;
		private int groundspawns = 3;
		private int jumpoverobstacles = 4;
		private int boxcoins = 5;

		// Chance Generator
		public static Generator Chances { get; set; }

		// RoadBarrier
		private int roadBarrierCount;
		private int pickupCount;

		// BoxRow
		private int rowStartBlock;
		private int rowLineStart;
		private bool rowBoxes = false;

		// Moving car
		private int movingStartBlock;
		private int movingLineStart;
		private bool movingCar = false;

		private int guideline; // lijn met muntjes voor de speler om te volgen mits een stuk baan leeg is
		private bool hascoins = false;

		private void Awake()
		{
			Chances = new Generator();
			StartCoroutine(CoroutineHelper.For(
				12.5f,
				() => 25,
				i => i <= 75,
				(ref int i) => i += 3,
				i => Chances.Fill(i, 20, 25, 75 - i)));
		}

		private GameObject JumpableObstacle()
		{
			return RandomUtilities.Pick(cone, bin, container, alternativeBox, singleBarrier);
		}

		private GameObject MiddleJumpableObstacle()
		{
			return RandomUtilities.Pick(cone, alternativeBox, singleBarrier);
		}

		private SpawnObject SetPickupOrPowerup(TilePopulation tilePopulation, SpawnObject pickup, int block)
		{
			if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.PickupChance) && pickupCount < maxPowerup)
			{
				pickup = GetPickup(pickup, block);
			}
			else if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.CoinChance))
			{
				pickup.Object = CoinChance();
			}
			else
			{
				pickup.Alive = false;
			}
			return pickup;
		}

		private SpawnObject BoxPickupOrPowerup(TilePopulation tilePopulation, SpawnObject pickup, int block)
		{
			// kans pickupspawn boven op dozen, heel veel spawnpunten achter elkaar dus kans lager anders ineens 3/4 powerups
			if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.PickupChance / 4) && pickupCount < maxPowerup)
			{
				pickup.Object = PowerupChance(block);
				pickupCount++;
			}
			else
			{
				pickup.Object = CoinChance();
			}
			return pickup;
		}

		private SpawnObject GetPickup(SpawnObject pickup, int block)
		{
			// How many pickups per straight tile.
			if (pickupCount < maxPowerup)
			{
				pickup.Object = PowerupChance(block);
				pickupCount++;
			}
			else
			{
				pickup.Alive = false;
			}

			return pickup;
		}

		private bool RowChance()
		{
			return RandomUtilities.PercentageChance(80 / (Chances.SpawnObstacle.ObstacleChance / 4)); // kans op rij dozen
		}

		private bool MovingChance()
		{
			return RandomUtilities.PercentageChance(Chances.SpawnObstacle.ObstacleChance / 3); // kans op een bewegende auto
		}

		private GameObject CoinChance()
		{
			if (!hascoins)
			{
				hascoins = true;
			}
			return RandomUtilities.WeightedPick(
				diamond.ToWeightedItem(1),
				coin.ToWeightedItem(20));
		}

		private GameObject PowerupChance(int block)
		{
			if (block < 3)
			{
				return RandomUtilities.WeightedPick(
					heart.ToWeightedItem(1),
					slowmotion.ToWeightedItem(40),
					inhaler.ToWeightedItem(100),
					doubleCoins.ToWeightedItem(150));
			}
			else
			{
				return RandomUtilities.WeightedPick(
					heart.ToWeightedItem(1),
					inhaler.ToWeightedItem(100));
			}
		}

		private GameObject ObstacleChance()
		{
			return RandomUtilities.WeightedPick(
				JumpableObstacle().ToWeightedItem(25),
				pizzaTruck.ToWeightedItem(1),
				hotdogTruck.ToWeightedItem(1));
		}

		private GameObject MiddleObstacleChance()
		{
			return RandomUtilities.WeightedPick(
				MiddleJumpableObstacle().ToWeightedItem(25),
				pizzaTruck.ToWeightedItem(1),
				hotdogTruck.ToWeightedItem(1));
		}

		private GameObject ObstacleOrBoth(int rownumber)
		{
			if (RowChance() && !rowBoxes)
			{
				return box;
			}
			else if (MovingChance() && !movingCar)
			{
				return RandomUtilities.Pick(cartoonCar, jeep);
			}
			else
			{
				if (rownumber == 1)
				{
					return MiddleObstacleChance();
				}
				else
				{
					return ObstacleChance();
				}
			}
		}

		private GameObject ObstacleOrMoving(int rownumber)
		{
			if (MovingChance() && !movingCar)
			{
				return RandomUtilities.Pick(cartoonCar, jeep);
			}
			else
			{
				if (rownumber == 1)
				{
					return MiddleObstacleChance();
				}
				else
				{
					return ObstacleChance();
				}
			}
		}

		private GameObject ObstacleOrRow(int rownumber)
		{
			if (RowChance() && !rowBoxes)
			{
				return box;
			}
			else
			{
				if (rownumber == 1)
				{
					return MiddleObstacleChance();
				}
				else
				{
					return ObstacleChance();
				}
			}
		}

		private GameObject Obstacle(int rownumber)
		{
			if (rownumber == 1)
			{
				return MiddleObstacleChance();
			}
			else
			{
				return ObstacleChance();
			}
		}

		private int RowLength()
		{
			return RandomUtilities.Generator.Next(1, 5 - rowStartBlock);
		}

		private int MovingLength()
		{
			return RandomUtilities.Generator.Next(1, movingStartBlock);
		}

		public void Populate(StraightTile tile)
		{
			var tilePopulation = new TilePopulation(tile);
			Assign(tilePopulation);
		}

		private void Assign(TilePopulation tilePopulation)
		{
			// assign each spawnobject to corresponding transform

			StartCoroutine(CoroutineHelper.For(
				0.1f,
				() => 1,
				o => o < 6,
				(ref int o) => o++,
				o =>
				{
					SpawnObject[][,] Block = new SpawnObject[6][,];
					tilePopulation.SpawnObstacles = new SpawnObject[1, 3];
					tilePopulation.GroundSpawns = new SpawnObject[3, 5];
					tilePopulation.JumpOverObstacles = new SpawnObject[3, 5];
					tilePopulation.BoxCoins = new SpawnObject[3, 6];
					tilePopulation.Boxes = new SpawnObject[3, 3];
					tilePopulation.AirSpawns = new SpawnObject[1, 3];

					for (int i = 0; i < 3; i++)
					{
						tilePopulation.SpawnObstacles[0, i] = new SpawnObject();
						tilePopulation.SpawnObstacles[0, i].Location = tilePopulation.Tile.transform.Find(
							String.Format("Spawner/Block {0}/Obstacles/SpawnObstacle{1}", o, i + 1));
						tilePopulation.AirSpawns[0, i] = new SpawnObject();
						tilePopulation.AirSpawns[0, i].Location = tilePopulation.Tile.transform.Find(
							String.Format("Spawner/Block {0}/AirSpawns/AirSpawn{1}", o, i + 1));

						for (int e = 0; e < 3; e++)
						{
							tilePopulation.Boxes[i, e] = new SpawnObject();
							tilePopulation.Boxes[i, e].Location = tilePopulation.Tile.transform.Find(
								String.Format("Spawner/Block {0}/Boxes/Line {1}/Box{2}", o, i + 1, e + 1));
						}

						for (int e = 0; e < 5; e++)
						{
							tilePopulation.GroundSpawns[i, e] = new SpawnObject();
							tilePopulation.GroundSpawns[i, e].Location = tilePopulation.Tile.transform.Find(
								String.Format("Spawner/Block {0}/GroundSpawns/Line {1}/GroundSpawn{2}", o, i + 1, e + 1));
							tilePopulation.JumpOverObstacles[i, e] = new SpawnObject();
							tilePopulation.JumpOverObstacles[i, e].Location = tilePopulation.Tile.transform.Find(
								String.Format("Spawner/Block {0}/JumpOverObstacle/Line {1}/AirObstacleSpawn{2}", o, i + 1, e + 1));
						}

						if (o >= 2 && o <= 4)
						{
							for (int e = 0; e < 6; e++)
							{
								tilePopulation.BoxCoins[i, e] = new SpawnObject();
								tilePopulation.BoxCoins[i, e].Location = tilePopulation.Tile.transform.Find(
									String.Format("Spawner/Block {0}/BoxCoins/Line {1}/BoxCoin{2}", o, i + 1, e + 1));
							}
						}
					}

					tilePopulation.MoveTrigger = new SpawnObject();
					tilePopulation.MoveTrigger.Object = moveTrigger;

					Block[0] = tilePopulation.SpawnObstacles;
					Block[1] = tilePopulation.AirSpawns;
					Block[2] = tilePopulation.Boxes;
					Block[3] = tilePopulation.GroundSpawns;
					Block[4] = tilePopulation.JumpOverObstacles;
					Block[5] = tilePopulation.BoxCoins;

					tilePopulation.Blocks[o - 1] = Block;

					if (o == 5)
					{
						FillStatic(tilePopulation);
					}
				}));
		}

		private void FillStatic(TilePopulation tilePopulation)
		{
			pickupCount = 0;
			for (int block = 0; block < 5; block++)
			{
				if (block % 1 == 0)
				{
					guideline = RandomUtilities.Pick(0, 1, 2);
				}

				for (int i = 0; i < tilePopulation.Blocks[block][spawnobstacles].GetLength(1); i++)
				{
					if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.ObstacleChance))
					{
						if (block == 0)
						{
							tilePopulation.Blocks[block][spawnobstacles][0, i].Object = Obstacle(i);
						}
						else if (block > 0 && block < 4)
						{
							if (block > 1)
							{
								tilePopulation.Blocks[block][spawnobstacles][0, i].Object = ObstacleOrBoth(i);
							}
							else
							{
								tilePopulation.Blocks[block][spawnobstacles][0, i].Object = ObstacleOrRow(i);
							}
						}
						else
						{
							tilePopulation.Blocks[block][spawnobstacles][0, i].Object = ObstacleOrMoving(i);
						}

						if (tilePopulation.Blocks[block][spawnobstacles][0, i].Object == box && !rowBoxes)
						{
							rowBoxes = true;
							rowStartBlock = block;
						}

						if ((tilePopulation.Blocks[block][spawnobstacles][0, i].Object == cartoonCar || tilePopulation.Blocks[block][spawnobstacles][0, i].Object == jeep) && !movingCar)
						{
							movingCar = true;
							movingStartBlock = block;
						}
					}
					else if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.PickupChance))
					{
						tilePopulation.Blocks[block][spawnobstacles][0, i] = GetPickup(tilePopulation.Blocks[block][spawnobstacles][0, i], block);
					}
					else
					{
						tilePopulation.Blocks[block][spawnobstacles][0, i].Alive = false;
					}
				}

				// jump or not jump over obstacle, pickups
				for (int i = 0; i < 3; i++)
				{
					SpawnObject obstacle = tilePopulation.Blocks[block][spawnobstacles][0, i];
					if ((obstacle.Object == cone ||
						obstacle.Object == bin ||
						obstacle.Object == container ||
						obstacle.Object == alternativeBox ||
						obstacle.Object == singleBarrier) &&
						obstacle.Alive)
					{
						if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.CoinChance))
						{
							for (int a = 0; a < 5; a++)
							{
								tilePopulation.Blocks[block][jumpoverobstacles][i, a].Object = CoinChance();
								tilePopulation.Blocks[block][groundspawns][guideline, a].Alive = false;
							}

							tilePopulation.Blocks[block][airspawns][0, i].Alive = false;
						}
						else if (RandomUtilities.PercentageChance(Chances.SpawnObstacle.PickupChance))
						{
							for (int a = 0; a < 5; a++)
							{
								tilePopulation.Blocks[block][jumpoverobstacles][i, a].Alive = false;
								tilePopulation.Blocks[block][groundspawns][guideline, a].Alive = false;
							}

							tilePopulation.Blocks[block][airspawns][0, i] = GetPickup(tilePopulation.Blocks[block][airspawns][0, i], block);
						}
					}
					else if (!obstacle.Alive && obstacle.Object == null)
					{
						for (int b = 0; b < tilePopulation.Blocks[block][jumpoverobstacles].GetLength(1); b++)
						{
							tilePopulation.Blocks[block][jumpoverobstacles][i, b].Alive = false;
						}

						tilePopulation.Blocks[block][airspawns][0, i].Alive = false;
					}
				}
				if (tilePopulation.Blocks[block][spawnobstacles][0, guideline].Object == null &&
					RandomUtilities.PercentageChance(Chances.SpawnObstacle.CoinChance))
				{
					for (int b = 0; b < tilePopulation.Blocks[block][groundspawns].GetLength(1); b++)
					{
						tilePopulation.Blocks[block][groundspawns][guideline, b].Object = CoinChance();
						tilePopulation.Blocks[block][spawnobstacles][0, guideline].Alive = true;
						tilePopulation.Blocks[block][spawnobstacles][0, guideline].Object = CoinChance();
					}
				}
			}

			if (rowBoxes)
			{
				FillBoxRows(tilePopulation);
			}

			if (movingCar)
			{
				FillMoving(tilePopulation);
			}

			FillBigObstacles(tilePopulation);
			JustifyCoindoublers(tilePopulation);
			Spawn(tilePopulation);
		}

		private void FillBoxRows(TilePopulation tilePopulation)
		{
			for (int e = 1; e < 4; e++)
			{
				for (int i = 0; i < 3; i++)
				{
					if (tilePopulation.Blocks[e][spawnobstacles][0, i].Object == box)
					{
						rowLineStart = i;
					}
				}
			}

			int rowLength = RowLength();
			if (rowStartBlock != 0)
			{
				tilePopulation.Blocks[rowStartBlock - 1][spawnobstacles][0, rowLineStart].Alive = false;
				tilePopulation.Blocks[rowStartBlock - 1][airspawns][0, rowLineStart].Alive = false;

				for (int airspawncount = 0; airspawncount < 5; airspawncount++)
				{
					tilePopulation.Blocks[rowStartBlock - 1][jumpoverobstacles][rowLineStart, airspawncount].Alive = false;
					tilePopulation.Blocks[rowStartBlock + rowLength][jumpoverobstacles][rowLineStart, airspawncount].Alive = false;
				}
			}
			tilePopulation.Blocks[rowStartBlock + rowLength][spawnobstacles][0, rowLineStart].Alive = false;
			tilePopulation.Blocks[rowStartBlock + rowLength][airspawns][0, rowLineStart].Alive = false;

			for (int block = rowStartBlock; block < (rowStartBlock + rowLength); block++)
			{
				for (int boxcount = 0; boxcount < 3; boxcount++)
				{
					tilePopulation.Blocks[block][boxes][rowLineStart, boxcount].Object = box;
					tilePopulation.Blocks[block][boxes][rowLineStart, boxcount].Alive = true;
				}

				tilePopulation.Blocks[block][spawnobstacles][0, rowLineStart].Alive = false;
				tilePopulation.Blocks[block][airspawns][0, rowLineStart].Alive = false;

				for (int groundspawncount = 0; groundspawncount < 5; groundspawncount++)
				{
					tilePopulation.Blocks[block][groundspawns][rowLineStart, groundspawncount].Alive = false;
					tilePopulation.Blocks[block][jumpoverobstacles][rowLineStart, groundspawncount].Alive = false;
				}

				for (int boxcoincount = 0; boxcoincount < 6; boxcoincount++)
				{
					if (tilePopulation.Blocks[block][boxcoins][rowLineStart, boxcoincount] != null)
					{
						tilePopulation.Blocks[block][boxcoins][rowLineStart, boxcoincount] = BoxPickupOrPowerup(
							tilePopulation, tilePopulation.Blocks[block][boxcoins][rowLineStart, boxcoincount], block);
						tilePopulation.Blocks[block][boxcoins][rowLineStart, boxcoincount].Alive = true;
					}
				}
			}
		}

		private void FillMoving(TilePopulation tilePopulation)
		{
			for (int b = 4; b > 1; b--)
			{
				for (int i = 0; i < 3; i++)
				{
					GameObject movingobject = tilePopulation.Blocks[b][spawnobstacles][0, i].Object;
					if ((movingobject == cartoonCar || movingobject == jeep) && !rowBoxes)
					{
						movingLineStart = i;
						movingStartBlock = b;
						tilePopulation.Blocks[b][spawnobstacles][0, i].Move = true;
					}
					else if ((movingobject == cartoonCar || movingobject == jeep) && i != rowLineStart && rowBoxes)
					{
						movingLineStart = i;
						movingStartBlock = b;
						tilePopulation.Blocks[b][spawnobstacles][0, i].Move = true;
					}
				}
			}

			int moveLength = MovingLength();
			int moveTriggerAdjustment = 2;
			if (movingStartBlock - moveLength - moveTriggerAdjustment < 0)
			{
				moveTriggerAdjustment = 1;
			}

			tilePopulation.MoveTrigger.Location = tilePopulation.Blocks[movingStartBlock - moveLength - moveTriggerAdjustment][groundspawns][1, 0].Location;

			for (int block = movingStartBlock - 1; block > (movingStartBlock - moveLength - 1); block--)
			{
				for (int boxcount = 0; boxcount < 3; boxcount++)
				{
					tilePopulation.Blocks[block][boxes][movingLineStart, boxcount].Alive = false;
				}

				if (movingLineStart == 0)
				{
					tilePopulation.Blocks[block][spawnobstacles][0, 0].Alive = false;
					tilePopulation.Blocks[block][airspawns][0, 0].Alive = false;
				}

				tilePopulation.Blocks[block][spawnobstacles][0, movingLineStart].Alive = false;
				tilePopulation.Blocks[block][airspawns][0, movingLineStart].Alive = false;
				for (int groundspawncount = 0; groundspawncount < 5; groundspawncount++)
				{
					tilePopulation.Blocks[block][jumpoverobstacles][movingLineStart, groundspawncount].Alive = false;
				}

				for (int boxcoincount = 0; boxcoincount < tilePopulation.Blocks[block][boxcoins].GetLength(1); boxcoincount++)
				{
					if (tilePopulation.Blocks[block][boxcoins][movingLineStart, boxcoincount] != null)
					{
						tilePopulation.Blocks[block][boxcoins][movingLineStart, boxcoincount].Alive = false;
					}
				}
			}
		}

		private void FillBigObstacles(TilePopulation tilePopulation)
		{
			for (int block = 0; block < 5; block++)
			{
				roadBarrierCount = 0;
				for (int i = 0; i < 3; i++)
				{
					GameObject obstacle = tilePopulation.Blocks[block][spawnobstacles][0, i].Object;
					if (tilePopulation.Blocks[block][spawnobstacles][0, i].Alive)
					{
						if (obstacle == cone ||
							obstacle == bin ||
							obstacle == container ||
							obstacle == pizzaTruck ||
							obstacle == hotdogTruck ||
							obstacle == alternativeBox ||
							obstacle == singleBarrier)
						{
							roadBarrierCount++;
						}
					}
				}

				if (roadBarrierCount == 3)
				{
					tilePopulation.Blocks[block][spawnobstacles][0, 0].Alive = false;
					tilePopulation.Blocks[block][spawnobstacles][0, 1].Object = roadBarrier;
					tilePopulation.Blocks[block][spawnobstacles][0, 2].Alive = false;

					for (int b = 0; b < tilePopulation.Blocks[block][groundspawns].GetLength(1); b++)
					{
						tilePopulation.Blocks[block][groundspawns][guideline, b].Alive = false;
					}
				}
			}
		}

		private void JustifyCoindoublers(TilePopulation tilePopulation)
		{
			if (!hascoins)
			{
				for (int block = 0; block < 5; block++)
				{
					var spawns = tilePopulation.Blocks[block][spawnobstacles].Cast<SpawnObject>()
						.Concat(tilePopulation.Blocks[block][airspawns].Cast<SpawnObject>())
						.Concat(tilePopulation.Blocks[block][boxcoins].Cast<SpawnObject>());
					foreach (var spawn in spawns)
					{
						if (spawn != null && spawn.Alive && spawn.Object == doubleCoins && spawn.Location != null)
						{
							spawn.Object = RandomUtilities.WeightedPick(
								heart.ToWeightedItem(2),
								slowmotion.ToWeightedItem(20),
								inhaler.ToWeightedItem(50));
						}
					}
				}
			}
		}

		private void Spawn(TilePopulation tilePopulation)
		{
			for (int blocknumber = 0; blocknumber < 5; blocknumber++)
			{
				var spawns = tilePopulation.Blocks[blocknumber][spawnobstacles].Cast<SpawnObject>()
						.Concat(tilePopulation.Blocks[blocknumber][groundspawns].Cast<SpawnObject>())
						.Concat(tilePopulation.Blocks[blocknumber][airspawns].Cast<SpawnObject>())
						.Concat(tilePopulation.Blocks[blocknumber][jumpoverobstacles].Cast<SpawnObject>())
						.Concat(tilePopulation.Blocks[blocknumber][airspawns].Cast<SpawnObject>())
						.Concat(tilePopulation.Blocks[blocknumber][boxcoins].Cast<SpawnObject>())
						.Concat(tilePopulation.Blocks[blocknumber][boxes].Cast<SpawnObject>());

				foreach (var spawn in spawns)
				{
					if (spawn != null && spawn.Alive && spawn.Object != null && spawn.Location != null)
					{
						Instantiate(
							spawn.Object,
							spawn.Location.position,
							spawn.Location.rotation,
							tilePopulation.Tile.transform.Find("Spawner"));
					}
				}
			}

			if (tilePopulation.MoveTrigger.Location != null)
			{
				GameObject parent = tilePopulation.Blocks[movingStartBlock][spawnobstacles][0, movingLineStart].Object;
				string location;

				if (parent == cartoonCar)
				{
					location = "Spawner/Car(Clone)";
				}
				else
				{
					location = "Spawner/Jeep(Clone)";
				}

				Instantiate(
						tilePopulation.MoveTrigger.Object,
						tilePopulation.MoveTrigger.Location.position,
						tilePopulation.MoveTrigger.Location.rotation,
						tilePopulation.Tile.transform.Find(location));
			}

			const string defaultName = "New Game Object";
			foreach (GameObject gameObject in FindObjectsOfType(typeof(GameObject)).Where(x => x.name == defaultName))
			{
				Destroy(gameObject);
			}

			movingCar = false;
			rowBoxes = false;
			hascoins = false;
			rowLineStart = new int();
			rowStartBlock = new int();
			movingLineStart = new int();
			movingStartBlock = new int();
			pickupCount = 0;
		}
	}
}
