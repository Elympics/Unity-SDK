using System;
using UnityEngine;
using Elympics;

namespace GameLogic.NewTicTacToe
{
	public class Field : MonoBehaviour, IStateSerializationHandler
	{
		public GameObject circle;
		public GameObject cross;

		public int Index { get; set; }

		public event Action<Field>             Clicked;
		public event Action<Field, PlayerSide> OwnershipChanged;

		private readonly ElympicsInt _ownership = new ElympicsInt((int) PlayerSide.None);

		public PlayerSide Ownership { get; private set; } = PlayerSide.None;

		public void OnPostStateDeserialize()
		{
			PlayerSide newOwnership = (PlayerSide)_ownership.Value;
			if (Ownership != newOwnership)
				SetOwnership(newOwnership);
		}

		public void OnPreStateSerialize()
		{
			_ownership.Value = (int)Ownership;
		}

		public void OnClick()
		{
			if (Ownership == PlayerSide.None)
				Clicked?.Invoke(this);
		}

		public void SetOwnership(PlayerSide playerSide)
		{
			if (Ownership != PlayerSide.None)
				return;
			if (playerSide == PlayerSide.Circle)
				circle.SetActive(true);
			else
				cross.SetActive(true);
			Ownership = playerSide;
			OwnershipChanged?.Invoke(this, playerSide);
		}
	}
}
