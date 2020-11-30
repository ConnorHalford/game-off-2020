using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
	Menu,
	Game,
	End
}

public class Game : MonoBehaviour
{
	private class Flight
	{
		public Spacecraft Craft = null;
		public float Timer = 0.0f;
		public float MaxTimer = 0.0f;
		public bool CraftMovementComplete = false;
		public bool NPCJumping = false;
		public bool QueueNextNPCMove = false;

		public float PercentRemaining { get { return Mathf.Max(0.0f, MaxTimer - Timer) / MaxTimer; } }
	}

	[Header("Game State")]
	[SerializeField] private float _minStateTime = 0.5f;
	[SerializeField] private int _shiftDuration = 300;
	[SerializeField] private float _noTimeTipMultiplier = 0.25f;
	[SerializeField] private float _tipHeightMultiplier = 3.0f;
	[SerializeField] private int _minTip = 999999;
	[SerializeField] private int _maxTip = 999999999;
	[SerializeField] private int _maxDrivableCraft = 15;
	[SerializeField] private Transform _playerSpawnPoint = null;
	[SerializeField] private float _minTimeBetweenArrivals = 3.0f;
	[SerializeField] private float _maxTimeBetweenArrivals = 10.0f;

	[Header("Spacecraft Arrival")]
	[SerializeField] private Spacecraft _prefabSpacecraft = null;
	[SerializeField] private BoxCollider _markerArrivals = null;
	[SerializeField] private float _arrivalDuration = 15.0f;
	[SerializeField] private float _minArrivalTimer = 30.0f;
	[SerializeField] private float _maxArrivalTimer = 50.0f;
	[SerializeField] private float _minSpacing = 3.0f;
	[SerializeField] private int _maxQueuedArrivals = 5;

	[Header("Spacecraft Departure")]
	[SerializeField] private BoxCollider _markerDepartures = null;
	[SerializeField] private float _minTimeBeforeDeparture = 20.0f;
	[SerializeField] private float _maxTimeBeforeDeparture = 60.0f;
	[SerializeField] private float _departureDuration = 15.0f;
	[SerializeField] private float _minDepartureTimer = 30.0f;
	[SerializeField] private float _maxDepartureTimer = 50.0f;
	[SerializeField] private int _maxQueuedDepartures = 5;

	[Header("NPCs")]
	[SerializeField] private float _npcJumpDuration = 1.0f;
	[SerializeField] private float _npcMoveDuration = 1.0f;
	[SerializeField] private Transform _npcArrivalJumpEnd = null;
	[SerializeField] private Transform _npcArrivalIndoors = null;
	[SerializeField] private Transform _npcDepartureIndoors = null;
	[SerializeField] private Transform _npcDepartureJumpStartMin = null;
	[SerializeField] private Transform _npcDepartureJumpStartMax = null;

	private List<Spacecraft> _pooledCraft = null;
	private List<Spacecraft> _allDrivableCraft = null;
	private List<float> _drivableTimers = null;

	private List<Flight> _arrivals = null;
	private Vector3 _arrivalStartPos = Vector3.zero;
	private Vector3 _arrivalEndPos = Vector3.zero;
	private float _arrivalTimeout = 0.0f;

	private List<Flight> _departures = null;
	private float _departureEndHeight = 0.0f;
	private Collider[] _collidersInDepartArea = new Collider[10];

	private GameState _state = GameState.Menu;
	private float _stateTimer = 0.0f;
	private ulong _credits = 0;
	private float _secondsRemaining = 0.0f;
	private int _numCraftParked = 0;
	private int _numCraftReturned = 0;

	public event System.Action<GameState> OnGameStateChanged = null;

	public List<Spacecraft> AllDrivableCraft { get { return _allDrivableCraft; } }
	public int MaxQueuedArrivals { get { return _maxQueuedArrivals; } }
	public int MaxQueuedDepartures { get { return _maxQueuedDepartures; } }
	public GameState State { get { return _state; } }
	public ulong Credits { get { return _credits; } }
	public float SecondsRemaining { get { return _secondsRemaining; } }
	public int NumCraftParked { get { return _numCraftParked; } }
	public int NumCraftReturned { get { return _numCraftReturned; } }

	private const float TIMER_LOCKED = 1000.0f;

	private void Awake()
	{
		Globals.RegisterGame(this);
		Globals.OnStartDriving += OnStartDriving;
	}

	private void Start()
	{
		_arrivalStartPos = _markerArrivals.transform.position + 0.5f * _markerArrivals.size.y * Vector3.up;
		_departureEndHeight = _markerDepartures.transform.position.y + 0.5f * _markerArrivals.size.y;

		_allDrivableCraft = new List<Spacecraft>(_maxDrivableCraft);
		_drivableTimers = new List<float>(_maxDrivableCraft);
		_arrivals = new List<Flight>(_maxQueuedArrivals);
		_departures = new List<Flight>(_maxQueuedDepartures);
		int numCraft = _maxQueuedArrivals + _maxQueuedDepartures + _maxDrivableCraft;
		_pooledCraft = new List<Spacecraft>(numCraft);
		for (int i = 0; i < numCraft; ++i)
		{
			MakeNewSpacecraft();
		}

		ChangeState(GameState.Menu);
	}

	private void MakeNewSpacecraft()
	{
		Quaternion spawnRot = Quaternion.LookRotation(Vector3.right, Vector3.up);
		Spacecraft craft = Instantiate(_prefabSpacecraft, _arrivalStartPos, spawnRot, transform);
		_arrivalEndPos = _markerArrivals.transform.position - (0.5f * _markerArrivals.size.y - craft.HeightWhenDriving) * Vector3.up;
		craft.gameObject.SetActive(false);
		_pooledCraft.Add(craft);
	}

	private void Update()
	{
		_stateTimer += Time.deltaTime;
		switch (_state)
		{
			case GameState.Menu:	UpdateMenu();	break;
			case GameState.Game:	UpdateGame();	break;
			case GameState.End:		UpdateEnd();	break;
		}
	}

	private void ChangeState(GameState state)
	{
		_state = state;
		_stateTimer = 0.0f;

		// Reset game state
		if (_state == GameState.Menu)
		{
			int count = _arrivals.Count;
			for (int i = 0; i < count; ++i)
			{
				_arrivals[i].Craft.gameObject.SetActive(false);
				_pooledCraft.Add(_arrivals[i].Craft);
			}
			_arrivals.Clear();

			count = _departures.Count;
			for (int i = 0; i < count; ++i)
			{
				_departures[i].Craft.gameObject.SetActive(false);
				_pooledCraft.Add(_departures[i].Craft);
			}
			_departures.Clear();

			count = _allDrivableCraft.Count;
			for (int i = 0; i < count; ++i)
			{
				_allDrivableCraft[i].gameObject.SetActive(false);
				_pooledCraft.Add(_allDrivableCraft[i]);
			}
			_allDrivableCraft.Clear();

			_credits = 0;
			_secondsRemaining = _shiftDuration + 0.99f;
			_numCraftParked = 0;
			_numCraftReturned = 0;

			Globals.Player.transform.position = _playerSpawnPoint.position;
		}

		if (OnGameStateChanged != null)
		{
			OnGameStateChanged(_state);
		}
	}

	private void UpdateMenu()
	{
		if (_stateTimer >= _minStateTime && Globals.Controls.Character.Jump.triggered)
		{
			ChangeState(GameState.Game);
		}
	}

	private void UpdateEnd()
	{
		if (_stateTimer >= _minStateTime && Globals.Controls.Character.Jump.triggered)
		{
			ChangeState(GameState.Menu);
		}
	}

	private void UpdateGame()
	{
		// PROGRESS DEPARTING CRAFT
		Vector3 maxDeltaPos = Vector3.up * Time.deltaTime * _departureEndHeight / _departureDuration;
		int numDepartures = _departures.Count;
		for (int i = numDepartures - 1; i >= 0; --i)
		{
			Flight flight = _departures[i];
			if (!flight.Craft.enabled && flight.NPCJumping && flight.QueueNextNPCMove)
			{
				// Progress
				flight.Timer += Time.deltaTime;
				flight.Craft.transform.position += maxDeltaPos;

				// FINISH CRAFT DEPARTURE
				if (flight.Craft.transform.position.y >= _departureEndHeight)
				{
					flight.CraftMovementComplete = true;
					flight.Craft.gameObject.SetActive(false);
					_pooledCraft.Add(flight.Craft);
					_departures.RemoveAt(i);
				}
			}
		}


		// CONVERT DRIVABLES INTO DEPARTURES
		if (_departures.Count < _maxQueuedDepartures)
		{
			int numDrivable = _allDrivableCraft.Count;
			for (int i = 0; i < numDrivable; ++i)
			{
				bool timedOut = _drivableTimers[i] <= 0.0f;
				if (!timedOut && _drivableTimers[i] < TIMER_LOCKED)
				{
					// Progress
					_drivableTimers[i] -= Time.deltaTime;

					// START NPC DEPARTURE
					if (_drivableTimers[i] <= 0.0f)
					{
						Flight depart = new Flight();
						Spacecraft craft = _allDrivableCraft[i];
						depart.Craft = craft;
						depart.Timer = 0.0f;
						depart.MaxTimer = Random.Range(_minDepartureTimer, _maxDepartureTimer);
						_departures.Add(depart);

						Vector3 jumpStartPos = Vector3.Lerp(_npcDepartureJumpStartMin.position,
							_npcDepartureJumpStartMax.position, Random.value);
						craft.NPC.MoveLinear(_npcDepartureIndoors.position, jumpStartPos, _npcMoveDuration,
							startAlpha: 0.0f, endAlpha: 1.0f, faceRight: false,
							onFinished: () => {
								depart.QueueNextNPCMove = true;
								Globals.UIManager.StartFlight(craft, arrival: false);
							});

						if (_departures.Count >= _maxQueuedDepartures)
						{
							break;
						}
					}
				}
			}
		}


		// PROGRESS DEPARTURE TIMERS
		numDepartures = _departures.Count;
		for (int departIndex = 0; departIndex < numDepartures; ++departIndex)
		{
			Flight flight = _departures[departIndex];
			if (flight.QueueNextNPCMove && !flight.NPCJumping && flight.Craft.enabled)
			{
				flight.Timer += Time.deltaTime;
				Globals.UIManager.UpdateSpacecraft(flight.Craft, flight.PercentRemaining, arrival: false);
			}
		}


		// MATCH NPCS TO CRAFT IN DEPARTURE AREA
		int numCraftInDepartArea = Physics.OverlapBoxNonAlloc(_markerDepartures.transform.position, 0.5f * _markerDepartures.size,
			_collidersInDepartArea, Quaternion.identity, Globals.MaskSpacecraft, QueryTriggerInteraction.Ignore);
		if (numCraftInDepartArea > 0 && numDepartures > 0)
		{
			for (int craftIndex = 0; craftIndex < numCraftInDepartArea; ++craftIndex)
			{
				Spacecraft craft = _collidersInDepartArea[craftIndex].GetComponentInParent<Spacecraft>();
				if (craft.IsDriving || !craft.enabled)
				{
					continue;
				}
				for (int departIndex = 0; departIndex < numDepartures; ++departIndex)
				{
					// START CRAFT DEPARTURE
					Flight flight = _departures[departIndex];
					if (flight.QueueNextNPCMove && !flight.NPCJumping && craft.IsEquivalent(flight.Craft))
					{
						// Score
						++_numCraftReturned;
						AddCredits(flight.PercentRemaining, flight.Craft.NPC.transform.position);

						// Make craft no longer drivable
						craft.DisableCollider();
						craft.SetOutlineVisible(false);
						craft.ResetModelRoll();
						craft.enabled = false;
						int numDrivable = _allDrivableCraft.Count;
						for (int driveIndex = 0; driveIndex < numDrivable; ++driveIndex)
						{
							if (_allDrivableCraft[driveIndex] == craft)
							{
								_allDrivableCraft.RemoveAt(driveIndex);
								_drivableTimers.RemoveAt(driveIndex);
								break;
							}
						}

						// Make NPC jump into craft
						Globals.UIManager.RemoveSpacecraft(flight.Craft, arrival: false);
						flight.NPCJumping = true;
						flight.QueueNextNPCMove = false;
						Vector3 offset = Globals.Player.ExitSpacecraftHeight * Vector3.up;
						flight.Craft.NPC.MoveArc(flight.Craft.NPC.transform.position, flight.Craft.transform, offset,
							_npcJumpDuration, faceRight: false,
							onFinished: () => {
								flight.QueueNextNPCMove = true;
								flight.Craft.NPC.Hide();
							});

						break;
					}
				}
			}
		}


		// PROGRESS ARRIVING CRAFT
		bool movementBlocked = Physics.CheckBox(_markerArrivals.transform.position, 0.5f * _markerArrivals.size,
			Quaternion.identity, Globals.MaskSpacecraft, QueryTriggerInteraction.Ignore);
		int numArrivals = _arrivals.Count;
		maxDeltaPos = Vector3.down * Time.deltaTime * (_arrivalStartPos.y - _arrivalEndPos.y) / _arrivalDuration;
		float heightPrevious = 0.0f;
		for (int i = 0; i < numArrivals; ++i)
		{
			// Work out whether the craft can move.
			// Bottommost arrival can only move if there are no drivable spacecraft in the arrival area.
			// Other arrivals can only move if they wouldn't end up too close to the arrival below them.
			Flight flight = _arrivals[i];
			bool canMove = (i > 0 || !movementBlocked) && !flight.CraftMovementComplete;
			if (canMove && i > 0)
			{
				float spacing = flight.Craft.transform.position.y - heightPrevious;
				canMove = spacing > _minSpacing;
			}

			// Progress
			flight.Timer += Time.deltaTime;
			Globals.UIManager.UpdateSpacecraft(flight.Craft, flight.PercentRemaining, arrival: true);
			bool justArrived = false;
			if (canMove)
			{
				Vector3 pos = flight.Craft.transform.position;
				pos += maxDeltaPos;
				float minY = heightPrevious + _minSpacing;
				if (i == 0)
				{
					minY = _arrivalEndPos.y;
					justArrived = pos.y <= minY;
				}
				pos.y = Mathf.Max(pos.y, minY);
				flight.Craft.transform.position = pos;
			}
			heightPrevious = flight.Craft.transform.position.y;

			// FINISH CRAFT ARRIVAL + START NPC ARRIVAL
			if (justArrived)
			{
				// Make craft drivable
				flight.CraftMovementComplete = true;
				flight.Craft.EnableCollider();
				flight.Craft.enabled = true;
				_allDrivableCraft.Add(flight.Craft);
				_drivableTimers.Add(TIMER_LOCKED + Random.Range(_minTimeBeforeDeparture, _maxTimeBeforeDeparture));

				// Make NPC jump out of craft
				flight.NPCJumping = true;
				flight.QueueNextNPCMove = false;
				Vector3 jumpStartPos = flight.Craft.transform.position + Globals.Player.ExitSpacecraftHeight * Vector3.up;
				flight.Craft.NPC.transform.position = jumpStartPos;
				flight.Craft.NPC.MoveArc(jumpStartPos, null, _npcArrivalJumpEnd.position, _npcJumpDuration, faceRight: true,
					onFinished: () => {
						flight.NPCJumping = false;
					});
			}
		}


		// FINISH NPC ARRIVAL
		if (_arrivals.Count > 0 && !_arrivals[0].NPCJumping)
		{
			Flight flight = _arrivals[0];
			flight.Craft.NPC.transform.position = _npcArrivalJumpEnd.position;
			if (flight.QueueNextNPCMove)
			{
				// Score
				++_numCraftParked;
				AddCredits(flight.PercentRemaining, flight.Craft.NPC.transform.position);

				// Unlock timer so craft's NPC can choose to depart
				int numDrivable = _allDrivableCraft.Count;
				for (int drive = 0; drive < numDrivable; ++drive)
				{
					if (_allDrivableCraft[drive] == _arrivals[0].Craft)
					{
						_drivableTimers[drive] -= TIMER_LOCKED;
						break;
					}
				}

				// Make NPC leave arrival area
				flight.Craft.NPC.MoveLinear(_npcArrivalJumpEnd.position, _npcArrivalIndoors.position, _npcMoveDuration,
					startAlpha: 1.0f, endAlpha: 0.0f, faceRight: true,
					onFinished: () => {
						flight.Craft.NPC.Hide();
					});

				Globals.UIManager.RemoveSpacecraft(flight.Craft, arrival: true);
				_arrivals.RemoveAt(0);
			}
		}


		// START NEW ARRIVALS
		_arrivalTimeout = Mathf.Max(0.0f, _arrivalTimeout - Time.deltaTime);
		if (_arrivalTimeout <= 0.0f && _arrivals.Count < _maxQueuedArrivals
			&& (_arrivals.Count + _allDrivableCraft.Count) < _maxDrivableCraft)
		{
			if (_pooledCraft.Count == 0)
			{
				MakeNewSpacecraft();
			}
			Spacecraft craft = _pooledCraft[_pooledCraft.Count - 1];
			_pooledCraft.RemoveAt(_pooledCraft.Count - 1);
			craft.gameObject.SetActive(true);
			craft.enabled = false;

			Quaternion spawnRot = Quaternion.LookRotation(Vector3.right, Vector3.up);
			craft.transform.SetPositionAndRotation(_arrivalStartPos, spawnRot);
			craft.Spawn();
			craft.DisableCollider();

			Flight flight = new Flight() { Craft = craft, Timer = 0.0f, MaxTimer = Random.Range(_minArrivalTimer, _maxArrivalTimer) };
			_arrivals.Add(flight);
			Globals.UIManager.StartFlight(craft, arrival: true);

			_arrivalTimeout = Random.Range(_minTimeBetweenArrivals, _maxTimeBetweenArrivals);
		}


		// SHIFT TIMER
		_secondsRemaining = Mathf.Max(0.0f, _secondsRemaining - Time.deltaTime);
		if (_secondsRemaining <= 0.0f)
		{
			ChangeState(GameState.End);
		}
	}

	private void AddCredits(float percentRemaining, Vector3 position)
	{
		float tipMultiplier = Mathf.Lerp(_noTimeTipMultiplier, 1.0f, percentRemaining);
		int tip = Random.Range(_minTip, _maxTip + 1);
		tip = Mathf.FloorToInt(tip * tipMultiplier);
		_credits += (ulong)tip;
		position += _tipHeightMultiplier * Globals.Player.ExitSpacecraftHeight * Vector3.up;
		Globals.UIManager.AddCredits(tip, position);
	}

	private void OnStartDriving(Spacecraft craft)
	{
		if (_arrivals.Count > 0 && craft == _arrivals[0].Craft)
		{
			_arrivals[0].QueueNextNPCMove = true;
		}
	}
}
