﻿using Assets.Scripts.Enumerations;
using Assets.Scripts.Extensions;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Models;
using System;
using Assets.Scripts.Helpers;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Controllers
{
	public class PlayerController : MonoBehaviour
	{
		private const float GRAVITY = 9.8f;

		[SerializeField]
		private float acceleration;

		[SerializeField]
		private float minSpeed;

		[SerializeField]
		private float maxSpeed;

		[SerializeField]
		private float jumpSpeed;

		private float currentSpeed;
		private Lane lane = Lane.Middle;
		private Animator animator;
		private CharacterController characterController;
		private float verticalSpeed = 0.0f;

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			animator = GetComponent<Animator>();
			currentSpeed = (maxSpeed + minSpeed) / 2;

			GameManager.Instance.Player = this;
			Orientation = Orientation.North;
		}

		public Orientation Orientation { get; private set; }

		public LanePosition[] TurningPositions { get; set; }

		public bool IsOnLeftCorner { get; set; }

		public bool IsOnRightCorner { get; set; }

		public bool IsOnCorner
		{
			get
			{
				return (IsOnLeftCorner || IsOnRightCorner) && TurningPositions != null && TurningPositions.Length > 0;
			}
		}

		public bool IsDamaged { get; private set; }
		public int Inhalers { get; internal set; }

		public int Coins { get; set; }

		public bool IsCoinDoublerActive { get; set; }

		private void Update()
		{
			if (IsDamaged)
			{
				return;
			}

			if ((IsOnLeftCorner && InputHelper.CornerLeft()) ||
				(IsOnRightCorner && InputHelper.CornerRight()))
			{
				TakeCorner();
			}
			else if (!IsOnCorner)
			{
				LaneSwapping();
			}
			
			Jump();
			Move();
		}

		private void Jump()
		{
			const float gravityMultiplier = 2.5f;

			if (IsAllowedToJump())
			{
				if (InputHelper.Jump())
				{
					verticalSpeed = jumpSpeed;
				}
				else
				{
					return;
				}
			}

			verticalSpeed -= gravityMultiplier * GRAVITY * Time.deltaTime;
			characterController.Move(verticalSpeed * Vector3.up * Time.deltaTime);
		}

		private bool IsAllowedToJump()
		{
			return IsTouchingGround(transform.position + Vector3.up * 0.6f) && !IsOnCorner && verticalSpeed <= 0;
		}

		private bool IsTouchingGround(Vector3 rayOrigin)
		{
			return Physics.Raycast(rayOrigin, Vector3.down, 0.2f);
		}

		public void TakeFailedCorner()
		{
			if (!IsOnCorner)
			{
				throw new InvalidOperationException("Tried to take invalid corner.");
			}

			IsDamaged = true;
			animator.SetFloat("Speed", 0.0f);
			currentSpeed = minSpeed;
			animator.Play("Damage");
			StartCoroutine(CoroutineHelper.Delay(0.5f, () => IsDamaged = false));

			TakeCorner();
		}

		private void TakeCorner()
		{
			// Set position
			var turningPosition = TurningPositions.OrderBy(x => Vector3.Distance(x.Position, transform.position)).First();
			transform.position = Orientation == Orientation.North || Orientation == Orientation.South
				? new Vector3(transform.position.x, transform.position.y, turningPosition.Position.z)
				: new Vector3(turningPosition.Position.x, transform.position.y, transform.position.z);
			lane = turningPosition.Lane;

			// Set rotation
			Orientation = IsOnLeftCorner ? Orientation.GetLeftOrientation() : Orientation.GetRightOrientation();
			iTween.RotateTo(gameObject, new Vector3(0, (int)Orientation * 90, 0), IsDamaged ? 1.5f : 0.5f);

			// Reset corner variables.
			IsOnLeftCorner = IsOnRightCorner = false;
			TurningPositions = null;
		}

		private void LaneSwapping()
		{
			if (lane != Lane.Left && InputHelper.LaneSwapLeft())
			{
				transform.position += Orientation.GetLeftOrientation().GetDirectionVector3() * Tile.LANE_DISTANCE;
				lane--;
			}
			else if (lane != Lane.Right && InputHelper.LaneSwapRight())
			{
				transform.position += Orientation.GetRightOrientation().GetDirectionVector3() * Tile.LANE_DISTANCE;
				lane++;
			}
		}

		private void Move()
		{
			if (currentSpeed < maxSpeed)
			{
				currentSpeed += acceleration;
				animator.SetFloat("Speed", currentSpeed);
			}

			transform.position += Orientation.GetDirectionVector3() * currentSpeed * Time.deltaTime;
		}
	}
}
