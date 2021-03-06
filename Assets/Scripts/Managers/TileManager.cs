﻿using Assets.Scripts.Enumerations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Utilities;
using Assets.Scripts.Helpers;
using Assets.Scripts.Models.Tiles;

namespace Assets.Scripts.Managers
{
	public class TileManager
		: MonoBehaviour
	{
		[SerializeField]
		private Transform startTile;

		[SerializeField]
		private Transform straightTile;

		[SerializeField]
		private Transform leftCornerTile;

		[SerializeField]
		private Transform rightCornerTile;

		private const int MAX_TILES = 10;
		private const int MAX_CORNERS = 4;
		private const int TILES_BEHIND_PLAYER = 2;
		private const double CORNER_CHANCE = RandomUtilities.HUNDRED_PERCENT * 2 / 3;

		private void Awake()
		{
			Instance = this;

			TilePopulator = GetComponent<TilePopulator>();
			Tiles = new List<Tile>(MAX_TILES + 1);
			ResetTiles();
		}

		public static TileManager Instance { get; private set; }

		public List<Tile> Tiles { get; private set; }

		private bool MayCreateCorner
		{
			get
			{
				if (Tiles.Last().GetComponent<Tile>().Type == TileType.Corner)
				{
					return false;
				}

				var reversed = Tiles.Reverse<Tile>().ToArray();
				if (reversed.Take(MAX_TILES / 2).Count(tile => IsCorner(tile)) >= MAX_CORNERS / 2 ||
					reversed.Take(MAX_TILES).Count(tile => IsCorner(tile)) >= MAX_CORNERS)
				{
					return false;
				}

				return true;
			}
		}

		public TilePopulator TilePopulator { get; private set; }

		private bool IsCorner(Tile tile)
		{
			return tile.Type == TileType.Corner;
		}

		public void ResetTiles()
		{
			while (Tiles.Count > 0)
			{
				var removeTile = Tiles[0];
				Destroy(removeTile.gameObject);
				Tiles.Remove(removeTile);
			}

			AddTile(startTile);
			StartCoroutine(CoroutineHelper.RepeatFor(0.2f, MAX_TILES - TILES_BEHIND_PLAYER - 1, AddRandomTile));
		}

		public void AddRandomTile()
		{
			var previousTile = Tiles.Last();
			if (RandomUtilities.PercentageChance(MayCreateCorner ? CORNER_CHANCE : 0))
			{
				var randomCorner = RandomUtilities.Pick(leftCornerTile, rightCornerTile);
				AddTile(randomCorner, previousTile, TileType.Corner);
			}
			else
			{
				AddTile(straightTile, previousTile, TileType.Regular);
			}

			if (Tiles.Count > MAX_TILES)
			{
				var removeTile = Tiles[0];
				Destroy(removeTile.gameObject);
				Tiles.Remove(removeTile);
			}
		}

		public void AddTile(Transform tilePrefab, Tile previousTile = null, TileType? type = null)
		{
			var tile = Instantiate(tilePrefab).GetComponent<Tile>();
			if (previousTile != null && type.HasValue)
			{
				tile.Construct(previousTile, type.Value);
			}
			Tiles.Add(tile);
		}
	}
}
