using Assets.Scripts.Enumerations;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Models.ScrollViewItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Controllers.ScrollViews
{
	class MatchHistoryController
		: MonoBehaviour
	{
		[SerializeField]
		private GameObject itemTemplate;

		[SerializeField]
		private GameObject itemHolder;

		private Match[] matches;

		private void UpdateMatches()
		{
			// TO-DO: Get matches from API.
			matches = new Match[0]
				.Where(match => match.Status == MatchStatus.Finished)
				//.OrderByDescending(match => match.CreatedOn)
				.ToArray();

			UpdateScrollView();
		}

		private void UpdateScrollView()
		{
			foreach (GameObject item in itemHolder.transform)
			{
				Destroy(item);
			}

			foreach (var match in matches)
			{
				GameObject scrollViewItem = Instantiate(itemTemplate, itemHolder.transform, false);
				scrollViewItem.GetComponent<MatchHistoryItem>().Match = match;
			}
		}
	}
}
