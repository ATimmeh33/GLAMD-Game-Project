using Assets.Scripts.Enumerations;
using Assets.Scripts.Extensions;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Models;
using System;
using Assets.Scripts.Helpers;
using Assets.Scripts.Managers;
using Assets.Scripts.Models.Tiles;
using Assets.Scripts.Models.Pickups;

namespace Assets.Scripts.Controllers
{
	public class PlayerController
		: MonoBehaviour
	{
		private const float GRAVITY = 9.8f;
		private const float INVINCIBLE_TIME_AFTER_DAMAGED = 1.5f;

		[SerializeField]
		private float acceleration;
		[SerializeField]
		private float minSpeed;
		[SerializeField]
		private float maxSpeed;
		[SerializeField]
		private float jumpSpeed;
		[SerializeField]
		private float laneSwapSpeed;
		[Range(0.1f, 1.0f)]
		[SerializeField]
		private float minimumLaneSwapSpeed;
		[SerializeField]
		private Models.Particle[] particles;

		private ParticleSystem instantiatedParticleSystem;
		private Lane lane = Lane.Middle;
		private Animator animator;
		private CharacterController characterController;
		private float verticalSpeed = 0.0f;
		private Vector3 targetLanePos;
		private bool reactivateCoinDoubler;
		private bool reactivateSlowmotion;
		private bool isTransitioningSlowmotion;

		private void Start()
		{
			GameManager.Instance.Player = this;
			CurrentTile = TileManager.Instance.Tiles.First();
			GetComponentInChildren<SwipeControl>().SetMethodToCall(OnSwipe);

			// Countdown.
			StartCoroutine(CoroutineHelper.WaitUntil(() => GameManager.Instance.GuiManager != null, () =>
			{
				StartCoroutine(CoroutineHelper.For(
				1,
				() => 3,
				i => i >= -1,
				(ref int i) => i--,
				i =>
				{
					GameManager.Instance.GuiManager.DisplayStartSignal(i);
					Frozen = i >= 0;
				}));
			}));

			if (GameManager.Instance.CurrentGame.GameType == GameType.MultiplayerCreate)
			{
				StartCoroutine(CoroutineHelper.Repeat(ReplayGhostController.REPLAY_INTERVAL,
					AddReplayDataPoint,
					() => Lives > 0,
					AddReplayDataPoint));
			}
		}

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			animator = GetComponent<Animator>();
			CurrentSpeed = (maxSpeed + minSpeed) / 2;
			Orientation = Orientation.North;
			Frozen = true;

			// Add points passively
			StartCoroutine(CoroutineHelper.Repeat(
				1.5f,
				() =>
				{
					if (!Frozen)
					{
						Points += 2;
					}
				},
				() => Lives > 0));
		}

		private int _lives = 3;
		public int Lives
		{
			get
			{
				return _lives;
			}
			set
			{
				if (GameManager.Instance.CurrentGame.GameOver)
				{
					return;
				}

				GameManager.Instance.GuiManager.UpdateLives(value, value < Lives);

				if (value <= 0)
				{
					GameOver();
				}
				else if (value < _lives)
				{
					IsInvincible = true;
					StartCoroutine(CoroutineHelper.Delay(INVINCIBLE_TIME_AFTER_DAMAGED, () => IsInvincible = false));
				}

				_lives = value;
			}
		}

		private int _points = 0;
		public int Points
		{
			get
			{
				return _points;
			}
			set
			{
				if (GameManager.Instance.CurrentGame.GameOver)
				{
					return;
				}

				GameManager.Instance.GuiManager.UpdatePoints(_points = value);
			}
		}

		private int _coins = 0;
		public int Coins
		{
			get
			{
				return _coins;
			}
			set
			{
				if (GameManager.Instance.CurrentGame.GameOver)
				{
					return;
				}

				GameManager.Instance.GuiManager.UpdateCoins(_coins = value);
			}
		}

		private int _inhalers = 0;
		public int Inhalers
		{
			get
			{
				return _inhalers;
			}
			set
			{
				if (GameManager.Instance.CurrentGame.GameOver)
				{
					return;
				}

				GameManager.Instance.GuiManager.UpdateInhalerMeter((_inhalers = value) / (float)Inhaler.MAX_AMOUNT);
			}
		}

		public float CurrentSpeed { get; private set; }

		public Orientation Orientation { get; private set; }

		public LanePosition[] TurningPositions { get; set; }

		public bool IsOnLeftCorner { get; set; }

		public bool IsOnRightCorner { get; set; }

		public Tile CurrentTile { get; set; }

		public bool IsDamaged { get; private set; }

		public bool IsCoinDoublerActive { get; private set; }

		public bool InhalerPowerupActive { get; set; }

		public bool Frozen { get; private set; }

		public bool IsInvincible { get; private set; }

		public bool InhalerUsable
		{
			get
			{
				return Inhalers == Inhaler.MAX_AMOUNT && !InhalerPowerupActive;
			}
		}

		public bool IsOnCorner
		{
			get
			{
				return (IsOnLeftCorner || IsOnRightCorner) && TurningPositions != null && TurningPositions.Length > 0;
			}
		}

		private void Update()
		{
			// Don't do anything when frozen or paused.
			if (Frozen || GameManager.Instance.Paused)
			{
				return;
			}

			// Apply gravity even when damaged.
			ApplyGravity();
			if (IsDamaged)
			{
				return;
			}

			if ((IsOnLeftCorner && Input.GetKeyDown(KeyCode.A)) ||
				(IsOnRightCorner && Input.GetKeyDown(KeyCode.D)))
			{
				TakeCorner();
			}
			else
			{
				CheckForLaneSwap(Swipe.None);
			}

			MoveToCorrectLane();
			Move();
			CheckForJump(Swipe.None);
		}

		private void OnSwipe(SwipeControl.SWIPE_DIRECTION direction)
		{
			if (IsDamaged || Frozen || GameManager.Instance.Paused)
			{
				return;
			}

			switch (direction)
			{
				case SwipeControl.SWIPE_DIRECTION.SD_UP:
					CheckForJump(Swipe.Up);
					break;
				case SwipeControl.SWIPE_DIRECTION.SD_LEFT:
					if (IsOnLeftCorner)
					{
						TakeCorner();
					}
					else
					{
						CheckForLaneSwap(Swipe.Left);
					}

					break;
				case SwipeControl.SWIPE_DIRECTION.SD_RIGHT:
					if (IsOnRightCorner)
					{
						TakeCorner();
					}
					else
					{
						CheckForLaneSwap(Swipe.Right);
					}

					break;
				default:
					break;
			}
		}

		private void AddReplayDataPoint()
		{
			GameManager.Instance.CurrentGame.Replay.Add(new ReplayDataPoint
			{
				Index = GameManager.Instance.CurrentGame.Replay.Count,
				Orientation = Orientation,
				Position = transform.position
			});
		}

		private void OnDestroy()
		{
			Time.timeScale = 1;
		}

		private void GameOver()
		{
			GameManager.Instance.GuiManager.DisableUI();
			GameManager.Instance.CurrentGame.GameOver = true;

			switch (GameManager.Instance.CurrentGame.GameType)
			{
				case GameType.Singleplayer:
					GameManager.Instance.HandleFinishedSingleplayerGame();
					break;
				case GameType.MultiplayerCreate:
					GameManager.Instance.HandleFinishedMultiplayerCreateGame();
					break;
				case GameType.MultiplayerChallenge:
					GameManager.Instance.HandleFinishedMultiplayerChallengeGame();
					break;
				default:
					throw new InvalidOperationException("Invalid GameType");
			}
		}

		private void CheckForJump(Swipe swipe)
		{
			if ((swipe == Swipe.Up || Input.GetKeyDown(KeyCode.Space)) && IsAllowedToJump())
			{
				verticalSpeed = jumpSpeed;
				animator.Play("Jump");
			}
		}

		private void ApplyGravity()
		{
			const float gravityMultiplier = 7.5f;

			if (verticalSpeed > 0 || !IsTouchingGround())
			{
				verticalSpeed -= gravityMultiplier * GRAVITY * Time.deltaTime;
				characterController.Move(verticalSpeed * Vector3.up * Time.deltaTime);
			}
		}

		private bool IsAllowedToJump()
		{
			return !IsOnCorner && verticalSpeed <= 0 && IsTouchingGround();
		}

		private bool IsTouchingGround()
		{
			bool touching = Physics.Raycast(
				transform.position + Vector3.up * 0.6f,
				Vector3.down,
				0.2f,
				-1,
				QueryTriggerInteraction.Ignore);
			if (touching)
			{
				verticalSpeed = 0;
				if (CurrentSpeed > minSpeed && CurrentSpeed <= maxSpeed)
				{
					animator.Play("Run");
				}
			}

			return touching;
		}

		public void TakeFailedCorner()
		{
			if (!IsOnCorner)
			{
				throw new InvalidOperationException("Tried to take invalid corner.");
			}

			if (InhalerPowerupActive)
			{
				TakeCorner();
				return;
			}

			IsDamaged = true;
			animator.SetFloat("Speed", 0.0f);
			CurrentSpeed = minSpeed;

			if (!IsInvincible)
			{
				Lives--;
			}

			if (Lives > 0)
			{
				// Wait until player lands.
				StartCoroutine(CoroutineHelper.WaitUntil(IsTouchingGround, () =>
				{
					animator.Play("Damage");
					StartCoroutine(CoroutineHelper.Delay(0.75f, () => IsDamaged = false));

					TakeCorner();
				}));
			}
		}

		private void TakeCorner()
		{
			// Set position
			var turningPosition = TurningPositions.OrderBy(x => Vector3.Distance(x.Position, transform.position)).First();
			transform.position = Orientation == Orientation.North || Orientation == Orientation.South
				? new Vector3(transform.position.x, transform.position.y, turningPosition.Position.z)
				: new Vector3(turningPosition.Position.x, transform.position.y, transform.position.z);
			lane = turningPosition.Lane;
			targetLanePos = Orientation == Orientation.North || Orientation == Orientation.South
				? Vector3.forward.Multiply(transform.position)
				: Vector3.right.Multiply(transform.position);

			// Set rotation
			Orientation = IsOnLeftCorner ? Orientation.GetLeftOrientation() : Orientation.GetRightOrientation();
			iTween.RotateTo(gameObject, new Vector3(0, (int)Orientation * 90, 0), IsDamaged ? 1.5f : 0.5f);

			// Reset corner variables.
			IsOnLeftCorner = IsOnRightCorner = false;
			TurningPositions = null;
		}

		private void CheckForLaneSwap(Swipe swipe)
		{
			if (IsOnCorner)
			{
				return;
			}

			if (lane != Lane.Left && (swipe == Swipe.Left || Input.GetKeyDown(KeyCode.A)))
			{
				targetLanePos += Orientation.GetLeftOrientation().GetDirectionVector3() * Tile.LANE_DISTANCE;
				lane--;
			}
			else if (lane != Lane.Right && (swipe == Swipe.Right || Input.GetKeyDown(KeyCode.D)))
			{
				targetLanePos += Orientation.GetRightOrientation().GetDirectionVector3() * Tile.LANE_DISTANCE;
				lane++;
			}
		}

		private void MoveToCorrectLane()
		{
			float speed = laneSwapSpeed * Time.deltaTime * Mathf.Max(
				(CurrentSpeed - minSpeed) / (maxSpeed - minSpeed),
				InhalerPowerupActive ? 1 : minimumLaneSwapSpeed);
			if (Orientation == Orientation.North || Orientation == Orientation.South)
			{
				transform.position = transform.position.CreateNew(x: Mathf.MoveTowards(
					transform.position.x,
					targetLanePos.x,
					speed));
			}
			else
			{
				transform.position = transform.position.CreateNew(z: Mathf.MoveTowards(
					transform.position.z,
					targetLanePos.z,
					speed));
			}
		}

		private void Move()
		{
			float regularMax = maxSpeed;
			float acceleration = this.acceleration;
			if (InhalerPowerupActive)
			{
				acceleration *= 3;
				regularMax -= Inhaler.SPEED_BONUS;
			}
			else if (CurrentSpeed > maxSpeed)
			{
				acceleration *= 5;
			}

			CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, maxSpeed, acceleration * Time.deltaTime);
			animator.SetFloat("Speed", CurrentSpeed / regularMax);
			transform.position += Orientation.GetDirectionVector3() * CurrentSpeed * Time.deltaTime;
		}

		public void ActivateInhaler(float duration, float speedBonus)
		{
			const int steps = 100;

			GetComponentInChildren<SwipeControl>().CancelCurrentSwipe();

			if (InhalerUsable)
			{
				SoundManager.Instance.PlaySoundEffect(Sound.InhalerActivate);
				ActivateParticleSystem(ParticleType.Invincibility);
				InhalerPowerupActive = true;
				maxSpeed += speedBonus;
				StartCoroutine(CoroutineHelper.For(
					duration / steps,
					() => 0,
					i => i <= steps && Lives > 0,
					(ref int i) => i++,
					i => GameManager.Instance.GuiManager.UpdateInhalerMeter(1.0f - 1.0f / steps * i),
					() =>
					{
						Inhalers = 0;
						maxSpeed -= speedBonus;
						InhalerPowerupActive = false;
					}));
			}
		}

		public void ActivateSlowmotion(float duration, float slowmotionFactor)
		{
			const int steps = 100;

			if (isTransitioningSlowmotion)
			{
				isTransitioningSlowmotion = false;
			}
			else if (Mathf.Approximately(Time.timeScale, slowmotionFactor))
			{
				reactivateSlowmotion = true;
				return;
			}

			Time.timeScale = slowmotionFactor;
			StartCoroutine(CoroutineHelper.For(
				duration / steps,
				() => 0,
				i => i <= steps && Lives > 0,
				(ref int i) =>
				{
					if (reactivateSlowmotion)
					{
						i = 0;
						reactivateSlowmotion = false;
					}
					else
					{
						i++;
					}
				},
				i => GameManager.Instance.GuiManager.UpdateSlowmotionMeter(1.0f - 1.0f / steps * i),
				() =>
				{
					SoundManager.Instance.PlaySoundEffect(Sound.ReverseSlowmotion);
					isTransitioningSlowmotion = true;
					StartCoroutine(CoroutineHelper.Repeat(
						0.2f,
						() => Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1.0f, 0.1f),
						() => !Mathf.Approximately(1.0f, Time.timeScale) && isTransitioningSlowmotion,
						() => isTransitioningSlowmotion = false));
				}));
		}

		public void ActivateCoinDoubler(float duration)
		{
			const int steps = 100;

			if (IsCoinDoublerActive)
			{
				reactivateCoinDoubler = true;
				return;
			}

			IsCoinDoublerActive = true;
			StartCoroutine(CoroutineHelper.For(
				duration / steps,
				() => 0,
				i => i <= steps && Lives > 0,
				(ref int i) =>
				{
					if (reactivateCoinDoubler)
					{
						i = 0;
						reactivateCoinDoubler = false;
					}
					else
					{
						i++;
					}
				},
				i => GameManager.Instance.GuiManager.UpdateCoinDoublerMeter(1.0f - 1.0f / steps * i),
				() => IsCoinDoublerActive = false));
		}

		public void TakeObstacleDamage()
		{
			IsDamaged = true;
			Lives--;

			if (Lives > 0)
			{
				animator.Play("Damage");
				StartCoroutine(CoroutineHelper.Delay(0.75f, () => IsDamaged = false));
			}

			animator.SetFloat("Speed", 0.0f);
			CurrentSpeed = minSpeed;
		}

		public void ActivateParticleSystem(ParticleType type)
		{
			if (instantiatedParticleSystem != null &&
				instantiatedParticleSystem.isPlaying &&
				InhalerPowerupActive)
			{
				return;
			}

			var particleSystem = particles.Single(x => x.ParticleType == type).ParticleSystem;
			instantiatedParticleSystem = Instantiate(particleSystem);
			instantiatedParticleSystem.gameObject.SetActive(true);
			if (!instantiatedParticleSystem.main.loop)
			{
				Destroy(instantiatedParticleSystem.gameObject, instantiatedParticleSystem.main.duration);
			}

			instantiatedParticleSystem.transform.SetParent(transform);
			instantiatedParticleSystem.transform.localPosition = instantiatedParticleSystem.transform.position.Add(y: 0.75f);
			instantiatedParticleSystem.transform.localRotation = instantiatedParticleSystem.transform.rotation;
		}
	}
}
