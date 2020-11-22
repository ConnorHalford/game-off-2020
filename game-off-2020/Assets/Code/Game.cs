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

	[SerializeField] private Spacecraft _prefabSpacecraft = null;

	[Header("Spacecraft Arrival")]
	[SerializeField] private BoxCollider _markerArrivals = null;
	[SerializeField] private float _arrivalDuration = 15.0f;
	[SerializeField] private float _minArrivalTimer = 30.0f;
	[SerializeField] private float _maxArrivalTimer = 50.0f;
	[SerializeField] private float _minSpacing = 3.0f;
	[SerializeField] private int _maxQueuedArrivals = 5;

	[Header("NPC Arrival")]
	[SerializeField] private Transform _npcArrivalLand = null;
	[SerializeField] private float _npcArrivalJumpDuration = 1.0f;
	[SerializeField] private Transform _npcArrivalIndoors = null;
	[SerializeField] private float _npcArrivalMoveDuration = 1.0f;

	[Header("Departures")]
	[SerializeField] private BoxCollider _markerDepartures = null;

	private List<Spacecraft> _allDrivableCraft = new List<Spacecraft>();
	private List<Flight> _arrivals = new List<Flight>();
	private Vector3 _arrivalStartPos = Vector3.zero;
	private Vector3 _arrivalEndPos = Vector3.zero;

	public List<Spacecraft> AllDrivableCraft { get { return _allDrivableCraft; } }

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
		SpawnSpacecraft();
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
	}

	private void Update()
	{
		// Update flights in order of arrival
		bool movementBlocked = Physics.CheckBox(_markerArrivals.transform.position, 0.5f * _markerArrivals.size,
			Quaternion.identity, Globals.MaskSpacecraft, QueryTriggerInteraction.Ignore);
		int numArrivals = _arrivals.Count;
		Vector3 maxDeltaPos = Vector3.down * Time.deltaTime * (_arrivalStartPos.y - _arrivalEndPos.y) / _arrivalDuration;
		float heightPrevious = -_minSpacing;
		for (int i = 0; i < numArrivals; ++i)
		{
			// Work out whether the craft can move.
			// Bottommost arrival can only move if there are no drivable spacecraft in the arrival area.
			// Other arrivals can only move if they wouldn't end up too close to the arrival below them.
			Flight flight = _arrivals[i];
			bool canMove = (i > 0 || !movementBlocked) && !flight.CraftMovementComplete;
			if (canMove)
			{
				float spacing = flight.Craft.transform.position.y - heightPrevious;
				if (spacing < _minSpacing)
				{
					canMove = false;
				}
			}

			// Move
			flight.Timer += Time.deltaTime;
			bool justArrived = false;
			if (canMove)
			{
				flight.Craft.transform.position += maxDeltaPos;
				if (flight.Craft.transform.position.y <= _arrivalEndPos.y)
				{
					flight.Craft.transform.position = _arrivalEndPos;
					justArrived = true;
				}
			}
			heightPrevious = flight.Craft.transform.position.y;

			// Arrive
			if (justArrived)
			{
				flight.CraftMovementComplete = true;
				flight.Craft.EnableCollider();
				flight.Craft.enabled = true;
				_allDrivableCraft.Add(flight.Craft);

				flight.NPCJumping = true;
				flight.QueueNextNPCMove = false;
				Vector3 exitPos = flight.Craft.transform.position + Globals.Player.ExitSpacecraftHeight * Vector3.up;
				flight.Craft.NPC.transform.position = exitPos;
				flight.Craft.NPC.MoveArc(exitPos, _npcArrivalLand.position, _npcArrivalJumpDuration, faceRight: true,
					onFinished: () => {
						flight.NPCJumping = false;
					});
			}
		}

		// Complete arrival
		if (_arrivals.Count > 0 && !_arrivals[0].NPCJumping)
		{
			NPC npc = _arrivals[0].Craft.NPC;
			npc.transform.position = _npcArrivalLand.position;
			if (_arrivals[0].QueueNextNPCMove)
			{
				npc.MoveLinear(_npcArrivalLand.position, _npcArrivalIndoors.position, _npcArrivalMoveDuration,
					startAlpha: 1.0f, endAlpha: 0.0f, faceRight: true,
					onFinished: () => {
						npc.Hide();
					});

				// TODO add tip based on PercentageRemaining

				_arrivals.RemoveAt(0);
			}
		}

		// Constantly spawn if possible
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
