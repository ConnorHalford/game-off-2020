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

	private void LevitateAnchor(Transform anchor)
	{
		Vector3 groundPoint = new Vector3(anchor.position.x, 0.0f, anchor.position.z);
		if (Physics.Raycast(anchor.position, Vector3.up, out RaycastHit hit, float.MaxValue, Globals.MaskEnvironment))
		{
			groundPoint = hit.point;
		}
		else if (Physics.Raycast(anchor.position, Vector3.down, out hit, float.MaxValue, Globals.MaskEnvironment))
		{
			groundPoint = hit.point;
		}
		float force = 0.0f;
		float targetY = groundPoint.y + _targetHeight;
		float heightDelta = Mathf.Abs(anchor.position.y - targetY);
		if (anchor.position.y < targetY)
		{
			// Too low, move up
			force = _levitationSpeedUp;
		}
		else if (anchor.position.y > targetY)
		{
			// Too high, move down
			force = -_levitationSpeedDown;
		}
		force *= Time.deltaTime;
		_rb.AddForceAtPosition(force * Vector3.up, anchor.position, ForceMode.VelocityChange);



		if (anchor == _frontLeftAnchor)
		{
			groundFrontLeft = groundPoint;
			targetFrontLeft = groundPoint + _targetHeight * Vector3.up;
		}
		else if (anchor == _frontRightAnchor)
		{
			groundFrontRight = groundPoint;
			targetFrontRight = groundPoint + _targetHeight * Vector3.up;
		}
		else if (anchor == _rearLeftAnchor)
		{
			groundRearLeft = groundPoint;
			targetRearLeft = groundPoint + _targetHeight * Vector3.up;
		}
		else if (anchor == _rearRightAnchor)
		{
			groundRearRight = groundPoint;
			targetRearRight = groundPoint + _targetHeight * Vector3.up;
		}
	}

	private Vector3 targetFrontLeft;
	private Vector3 targetFrontRight;
	private Vector3 targetRearLeft;
	private Vector3 targetRearRight;
	private Vector3 groundFrontLeft;
	private Vector3 groundFrontRight;
	private Vector3 groundRearLeft;
	private Vector3 groundRearRight;

	private void OnDrawGizmos()
	{
		Gizmos.DrawSphere(_frontLeftAnchor.position, 0.1f);
		Gizmos.DrawSphere(_frontRightAnchor.position, 0.1f);
		Gizmos.DrawSphere(_rearLeftAnchor.position, 0.1f);
		Gizmos.DrawSphere(_rearRightAnchor.position, 0.1f);
		Gizmos.DrawSphere(targetFrontLeft, 0.1f);
		Gizmos.DrawSphere(targetFrontRight, 0.1f);
		Gizmos.DrawSphere(targetRearLeft, 0.1f);
		Gizmos.DrawSphere(targetRearRight, 0.1f);
		Gizmos.DrawSphere(groundFrontLeft, 0.1f);
		Gizmos.DrawSphere(groundFrontRight, 0.1f);
		Gizmos.DrawSphere(groundRearLeft, 0.1f);
		Gizmos.DrawSphere(groundRearRight, 0.1f);
		Gizmos.DrawLine(_frontLeftAnchor.position, targetFrontLeft);
		Gizmos.DrawLine(_frontRightAnchor.position, targetFrontRight);
		Gizmos.DrawLine(_rearLeftAnchor.position, targetRearLeft);
		Gizmos.DrawLine(_rearRightAnchor.position, targetRearRight);
		Gizmos.DrawLine(groundFrontLeft, targetFrontLeft);
		Gizmos.DrawLine(groundFrontRight, targetFrontRight);
		Gizmos.DrawLine(groundRearLeft, targetRearLeft);
		Gizmos.DrawLine(groundRearRight, targetRearRight);
	}

	public int LevitateMode = 0;

	private void FixedUpdate()
	{
		// Levitate
		if (LevitateMode == 0)
		{
			_levitationSpeedUp = 17.0f;
			_levitationSpeedDown = 10.0f;

			Vector3 groundPoint = new Vector3(_rb.position.x, 0.0f, _rb.position.z);
			if (Physics.Raycast(_rb.position, Vector3.up, out RaycastHit hit, float.MaxValue, Globals.MaskEnvironment))
			{
				groundPoint = hit.point;
			}
			else if (Physics.Raycast(_rb.position, Vector3.down, out hit, float.MaxValue, Globals.MaskEnvironment))
			{
				groundPoint = hit.point;
			}
			float targetY = groundPoint.y + _targetHeight;

			float y = _rb.position.y;
			float deltaY = Time.deltaTime * (y < targetY ? _levitationSpeedUp : _levitationSpeedDown);
			y = Mathf.SmoothStep(y, targetY, deltaY);
			_rb.position = new Vector3(_rb.position.x, y, _rb.position.z);
		}
		else if (LevitateMode == 1)
		{
			_levitationSpeedUp = 1.0f;
			_levitationSpeedDown = 1.0f;

			LevitateAnchor(_frontLeftAnchor);
			LevitateAnchor(_frontRightAnchor);
			LevitateAnchor(_rearLeftAnchor);
			LevitateAnchor(_rearRightAnchor);
		}

		// Rotate
		if (_driving)
		{
			float turn = _moveInput.x * _turnSpeed * Time.deltaTime;
			_rb.angularVelocity += turn * Vector3.up;
		}
		Quaternion roll = Quaternion.Euler(0.0f, 0.0f, _rb.angularVelocity.y * _rollAmount * Time.deltaTime);
		_modelRoot.localRotation = roll;

		// Drive
		if (_driving)
		{
			float force = (_moveInput.y > 0.0f ? _driveForceForward : _driveForceReverse) * _moveInput.y * Time.deltaTime;
			_rb.AddForce(force * transform.forward, ForceMode.VelocityChange);
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
}
