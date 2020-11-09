using UnityEngine;

public class Spacecraft : MonoBehaviour
{
	public enum Model
	{
		CargoA,
		CargoB,
		Miner,
		Racer,
		SpeederA,
		SpeederB,
		SpeederC,
		SpeederD
	}

#if UNITY_EDITOR
	private class DebugRaycastData
	{
		public Vector3 Start = Vector3.zero;
		public Vector3 End = Vector3.zero;
		public Vector3 Highlight = Vector3.zero;

		public void Set(Vector3 start, Vector3 end, Vector3 highlight)
		{
			Start = start;
			End = end;
			Highlight = highlight;
		}
	}
	private DebugRaycastData _debugRaycastFront = new DebugRaycastData();
	private DebugRaycastData _debugRaycastCenter = new DebugRaycastData();
	private DebugRaycastData _debugRaycastRear = new DebugRaycastData();

	[Header("Debug")]
	[SerializeField] private float _debugRadius = 0.1f;
	[SerializeField] private Color _debugLineColor = Color.white;
	[SerializeField] private Color _debugSphereColor = Color.white;
	[SerializeField] private Color _debugHighlightColor = Color.yellow;
#endif	// UNITY_EDITOR

	[Header("References")]
	[SerializeField] private Model _model = Model.CargoA;
	[SerializeField] private GameObject[] _modelGOs = new GameObject[System.Enum.GetValues(typeof(Model)).Length];
	[SerializeField] private Transform _modelRoot = null;

	[Header("Height")]
	[SerializeField] private Transform _frontLeftAnchor = null;
	[SerializeField] private Transform _frontRightAnchor = null;
	[SerializeField] private Transform _rearLeftAnchor = null;
	[SerializeField] private Transform _rearRightAnchor = null;
	[SerializeField] private float _heightWhenDriving = 0.5f;
	[SerializeField] private float _heightWhenParked = 0.1f;
	[SerializeField] private float _levitationSpeedUp = 5.0f;
	[SerializeField] private float _levitationSpeedDown = 1.0f;

	[Header("Turning")]
	[SerializeField] private float _turnSpeed = 5.0f;
	[SerializeField] private float _rollAmount = 1.0f;

	[Header("Driving")]
	[SerializeField] private float _driveForceForward = 60.0f;
	[SerializeField] private float _driveForceReverse = 30.0f;
	[SerializeField] private float _maxForwardSpeed = 10.0f;
	[SerializeField] private float _maxReverseSpeed = 1.0f;

	private Rigidbody _rb = null;
	private bool _driving = false;
	private float _targetHeight = 0.0f;
	private Vector2 _moveInput = Vector2.zero;
	private Model _modelPrevFrame = Model.CargoA;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		ChangeModel(_model);
	}

	private void Update()
	{
		// Change model
		if (_model != _modelPrevFrame)
		{
			ChangeModel(_model);
		}

		// Start/stop driving
		if (Globals.Controls.Character.EnterVehicle.triggered)
		{
			if (_driving)
			{
				StopDriving();
			}
			else
			{
				StartDriving();
			}
		}

		// Read input
		_moveInput = Vector2.zero;
		if (_driving)
		{
			_moveInput = Globals.Controls.Character.Movement.ReadValue<Vector2>();
		}
	}

	private void LateUpdate()
	{
		ResetUnwantedRotations();
	}

	private Vector3 CalcGroundPoint(Vector3 position)
	{
		Vector3 groundPoint = new Vector3(position.x, 0.0f, position.z);
		if (Physics.Raycast(position, Vector3.up, out RaycastHit hit, float.MaxValue, Globals.MaskEnvironment))
		{
			groundPoint = hit.point;
		}
		else if (Physics.Raycast(position, Vector3.down, out hit, float.MaxValue, Globals.MaskEnvironment))
		{
			groundPoint = hit.point;
		}
		return groundPoint;
	}

	private void FixedUpdate()
	{
		// Levitate
		Vector3 frontAnchor = 0.5f * (_frontLeftAnchor.position + _frontRightAnchor.position);
		Vector3 rearAnchor = 0.5f * (_rearLeftAnchor.position + _rearRightAnchor.position);
		Vector3 frontGround = CalcGroundPoint(frontAnchor);
		Vector3 rearGround = CalcGroundPoint(rearAnchor);
		Vector3 centerGround = CalcGroundPoint(_rb.position);
		Vector3 frontTarget = frontGround + _targetHeight * Vector3.up;
		Vector3 rearTarget = rearGround + _targetHeight * Vector3.up;
		Vector3 rearToFront = frontTarget - rearTarget;
		float targetY = Mathf.Max((rearTarget + 0.5f * rearToFront).y, centerGround.y + _targetHeight);
		float y = _rb.position.y;
		float deltaY = Time.deltaTime * (y < targetY ? _levitationSpeedUp : _levitationSpeedDown);
		y = Mathf.SmoothStep(y, targetY, deltaY);
		_rb.position = new Vector3(_rb.position.x, y, _rb.position.z);

#if UNITY_EDITOR
		_debugRaycastFront.Set(frontAnchor, frontGround, frontTarget);
		_debugRaycastRear.Set(rearAnchor, rearGround, rearTarget);
		_debugRaycastCenter.Set(0.5f * (frontAnchor + rearAnchor), centerGround, _rb.position);
#endif	// UNITY_EDITOR

		// Rotate
		if (_driving)
		{
			float turn = _moveInput.x * _turnSpeed * Time.deltaTime;
			_rb.angularVelocity += turn * Vector3.up;
		}
		Vector3 pitchAndYaw = Quaternion.LookRotation(rearToFront, Vector3.up).eulerAngles;
		float roll = _rb.angularVelocity.y * _rollAmount * Time.deltaTime;
		Quaternion modelRotation = Quaternion.Euler(pitchAndYaw.x, pitchAndYaw.y, roll);
		_modelRoot.rotation = modelRotation;

		// Drive
		if (_driving)
		{
			float force = (_moveInput.y > 0.0f ? _driveForceForward : _driveForceReverse) * _moveInput.y * Time.deltaTime;
			Vector3 direction = new Vector3(_modelRoot.forward.x, 0.0f, _modelRoot.forward.z);	// note not normalized when not horizontal
			_rb.AddForce(force * direction, ForceMode.VelocityChange);
		}

		// Cap horizontal speed
		Vector3 velocity = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z);
		bool movingForward = Vector3.Dot(velocity, transform.forward) > 0.0f;
		float maxSpeed = movingForward ? _maxForwardSpeed : _maxReverseSpeed;
		if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
		{
			velocity = velocity.normalized * maxSpeed;
			_rb.velocity = new Vector3(velocity.x, _rb.velocity.y, velocity.z);
		}

		ResetUnwantedRotations();
	}

	private void ResetUnwantedRotations()
	{
		Vector3 rotation = transform.eulerAngles;
		if (_rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX))
		{
			rotation = new Vector3(0.0f, rotation.y, rotation.z);
		}
		if (_rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY))
		{
			rotation = new Vector3(rotation.x, 0.0f, rotation.z);
		}
		if (_rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationZ))
		{
			rotation = new Vector3(rotation.x, rotation.y, 0.0f);
		}
		transform.eulerAngles = rotation;
	}

	private void StartDriving()
	{
		_targetHeight = _heightWhenDriving;
		_driving = true;
	}

	private void StopDriving()
	{
		_targetHeight = _heightWhenParked;
		_driving = false;
	}

	private void ChangeModel(Model model)
	{
		_model = model;
		_modelPrevFrame = _model;
		_modelGOs[(int)Model.CargoA].SetActive(_model == Model.CargoA);
		_modelGOs[(int)Model.CargoB].SetActive(_model == Model.CargoB);
		_modelGOs[(int)Model.Miner].SetActive(_model == Model.Miner);
		_modelGOs[(int)Model.Racer].SetActive(_model == Model.Racer);
		_modelGOs[(int)Model.SpeederA].SetActive(_model == Model.SpeederA);
		_modelGOs[(int)Model.SpeederB].SetActive(_model == Model.SpeederB);
		_modelGOs[(int)Model.SpeederC].SetActive(_model == Model.SpeederC);
		_modelGOs[(int)Model.SpeederD].SetActive(_model == Model.SpeederD);
	}

# if UNITY_EDITOR
	private void DrawDebugRaycast(DebugRaycastData raycast)
	{
		Gizmos.color = _debugSphereColor;
		Gizmos.DrawSphere(raycast.Start, _debugRadius);
		Gizmos.DrawSphere(raycast.End, _debugRadius);
		Gizmos.color = _debugLineColor;
		Gizmos.DrawLine(raycast.Start, raycast.End);
		Gizmos.DrawLine(raycast.Start, raycast.Highlight);
		Gizmos.DrawLine(raycast.Highlight, raycast.End);
		Gizmos.color = _debugHighlightColor;
		Gizmos.DrawSphere(raycast.Highlight, _debugRadius);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = _debugSphereColor;
		Gizmos.DrawSphere(_frontLeftAnchor.position, _debugRadius);
		Gizmos.DrawSphere(_frontRightAnchor.position, _debugRadius);
		Gizmos.DrawSphere(_rearLeftAnchor.position, _debugRadius);
		Gizmos.DrawSphere(_rearRightAnchor.position, _debugRadius);

		Gizmos.color = _debugLineColor;
		Gizmos.DrawLine(_frontLeftAnchor.position, _frontRightAnchor.position);
		Gizmos.DrawLine(_frontRightAnchor.position, _rearRightAnchor.position);
		Gizmos.DrawLine(_rearRightAnchor.position, _rearLeftAnchor.position);
		Gizmos.DrawLine(_rearLeftAnchor.position, _frontLeftAnchor.position);

		DrawDebugRaycast(_debugRaycastFront);
		DrawDebugRaycast(_debugRaycastCenter);
		DrawDebugRaycast(_debugRaycastRear);
	}
#endif	// UNITY_EDITOR
}
