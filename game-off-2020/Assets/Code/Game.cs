using System.Collections.Generic;
using UnityEngine;

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

	private List<Spacecraft> _allDrivableCraft = new List<Spacecraft>();
	private List<float> _drivableTimers = new List<float>();

	private List<Flight> _arrivals = new List<Flight>();
	private Vector3 _arrivalStartPos = Vector3.zero;
	private Vector3 _arrivalEndPos = Vector3.zero;

	private List<Flight> _departures = new List<Flight>();
	private float _departureEndHeight = 0.0f;
	private Collider[] _collidersInDepartArea = new Collider[10];

	public List<Spacecraft> AllDrivableCraft { get { return _allDrivableCraft; } }
	public int MaxQueuedArrivals { get { return _maxQueuedArrivals; } }
	public int MaxQueuedDepartures { get { return _maxQueuedDepartures; } }

	private const float TIMER_LOCKED = 1000.0f;

	private void Awake()
	{
		Globals.RegisterGame(this);
		Globals.OnStartDriving -= OnStartDriving;
		Globals.OnStartDriving += OnStartDriving;
	}

	private void OnDestroy()
	{
		Globals.OnStartDriving -= OnStartDriving;
	}

	private void Start()
	{
		_arrivalStartPos = _markerArrivals.transform.position + 0.5f * _markerArrivals.size.y * Vector3.up;
		_departureEndHeight = _markerDepartures.transform.position.y + 0.5f * _markerArrivals.size.y;
	}

	private void SpawnSpacecraft()
	{
		Quaternion spawnRot = Quaternion.LookRotation(Vector3.right, Vector3.up);
		Spacecraft craft = Instantiate(_prefabSpacecraft, _arrivalStartPos, spawnRot, transform);
		craft.Spawn();
		craft.DisableCollider();
		_arrivalEndPos = _markerArrivals.transform.position - (0.5f * _markerArrivals.size.y - craft.HeightWhenDriving) * Vector3.up;
		craft.enabled = false;
		Flight flight = new Flight() { Craft = craft, Timer = 0.0f, MaxTimer = Random.Range(_minArrivalTimer, _maxArrivalTimer) };
		_arrivals.Add(flight);
		Globals.UIManager.AddSpacecraft(craft, arrival: true);
	}

	private void Update()
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
					Destroy(flight.Craft.gameObject);	// TODO pooling
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
								Globals.UIManager.AddSpacecraft(craft, arrival: false);
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
						Vector3 jumpEndPos = flight.Craft.transform.position + Globals.Player.ExitSpacecraftHeight * Vector3.up;
						flight.Craft.NPC.MoveArc(flight.Craft.NPC.transform.position, jumpEndPos, _npcJumpDuration, faceRight: false,
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
				flight.Craft.NPC.MoveArc(jumpStartPos, _npcArrivalJumpEnd.position, _npcJumpDuration, faceRight: true,
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
		if (_arrivals.Count < _maxQueuedArrivals)
		{
			SpawnSpacecraft();
		}
	}

	private void OnStartDriving(Spacecraft craft)
	{
		if (_arrivals.Count > 0 && craft == _arrivals[0].Craft)
		{
			_arrivals[0].QueueNextNPCMove = true;
		}
	}
}
