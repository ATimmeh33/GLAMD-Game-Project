using Assets.Scripts.Enumerations;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Models.ScrollViewItems
{
	public class MatchHistoryItem
		: MonoBehaviour
	{
		[SerializeField]
		private Color winColor;

		[SerializeField]
		private Color drawColor;

		[SerializeField]
		private Color lossColor;

		[SerializeField]
		private Text lblScore;

		[SerializeField]
		private Text lblOpponentScore;

		[SerializeField]
		private Text lblOpponentName;

		[SerializeField]
		private Text lblResult;

		[SerializeField]
		private Image backgroundItem;

		private Match _match;
		public Match Match
		{
			get
			{
				return _match;
			}
			set
			{
				_match = value;
				UpdateItem();
			}
		}

		private void UpdateItem()
		{
			SetResult();
			SetScores();
		}

		private void SetScores()
		{
			if (Match.CreatorId == GameManager.Instance.User.Id)
			{
				lblOpponentName.text = Match.OpponentId.ToString();//Match.OpponentName;
				lblOpponentScore.text = Match.OpponentScore.ToString();
				lblScore.text = Match.CreatorScore.ToString();
			}
			else
			{
				lblOpponentName.text = Match.CreatorId.ToString();//Match.CreatorName;
				lblOpponentScore.text = Match.CreatorScore.ToString();
				lblScore.text = Match.OpponentScore.ToString();
			}
		}

		private void SetResult()
		{
			switch (Match.MatchWinner)
			{
				case MatchWinner.User:
					backgroundItem.color = winColor;
					lblResult.text = "Gewonnen";
					break;
				case MatchWinner.Opponent:
					backgroundItem.color = lossColor;
					lblResult.text = "Verloren";
					break;
				case MatchWinner.Draw:
					backgroundItem.color = drawColor;
					lblResult.text = "Gelijkspel";
					break;
				default:
					throw new InvalidOperationException("Invalid match winner status.");
			}
		}
	}
}
