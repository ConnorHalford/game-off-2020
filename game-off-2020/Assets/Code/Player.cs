using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
	[SerializeField] private SpriteRenderer _sprite = null;
	[SerializeField] private CharacterAnimator _anim = null;

	[Header("Movement")]
	[SerializeField] private float _moveForce = 30.0f;
	[SerializeField] private float _maxHorizontalSpeed = 10.0f;
	[SerializeField] private float _jumpForce = 60.0f;
	[SerializeField] private float _jumpGracePeriod = 0.1f;
	[SerializeField] private float _timeBetweenJumps = 0.1f;

	[Header("Anim")]
	[SerializeField] private float _raycastHeightOffset = 1.0f;
	[SerializeField] private float _heightConsideredGrounded = 0.2f;
	[SerializeField] private float _idleThreshold = 0.1f;
	[SerializeField] private float _spriteFlipThreshold = 0.01f;
	[SerializeField] private bool _spriteJumpsInScreenSpace = false;

	private Camera _camera = null;
	private Rigidbody _rb = null;
	private CapsuleCollider _collider = null;
	private Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

	private Controls _controls = null;
	private Vector2 _moveInput = Vector2.zero;
	private float _timeLastGrounded = 0.0f;
	private float _timeLastJumped = 0.0f;
	private float _timeLastPressedJump = 0.0f;

	private int _maskEnvironment = 0;

	private void Awake()
	{
		Application.targetFrameRate = 60;

		_controls = new Controls();
		_rb = GetComponent<Rigidbody>();
		_collider = GetComponent<CapsuleCollider>();
		_maskEnvironment = LayerMask.GetMask("Environment");
	}

	private void OnEnable()
	{
		_controls.Character.Enable();
		_sprite.flipX = false;
		_anim.PlayIdle();
		_timeLastGrounded = Time.time;
		_timeLastJumped = 0.0f;
		_timeLastPressedJump = 0.0f;
	}

	private void OnDisable()
	{
		_controls.Character.Disable();
	}

	private void Update()
	{
		_moveInput = _controls.Character.Movement.ReadValue<Vector2>();
		if (_controls.Character.Jump.triggered)
		{
			_timeLastPressedJump = Time.time;
		}
	}

	private void FixedUpdate()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}

		// Move
		MoveHorizontal();

		// Grounded check
		float heightAboveGround = transform.position.y;
		Vector3 basePosition = transform.position;
		Ray ray = new Ray(transform.position + _raycastHeightOffset * Vector3.up, Vector3.down);
		if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, _maskEnvironment))
		{
			if (hit.distance < _raycastHeightOffset + _heightConsideredGrounded)
			{
				_timeLastGrounded = Time.time;
			}
			heightAboveGround = hit.distance - _raycastHeightOffset;
			basePosition = hit.point;
		}
		else if (_groundPlane.Raycast(ray, out heightAboveGround))
		{
			basePosition = ray.origin + heightAboveGround * ray.direction;
			heightAboveGround -= _raycastHeightOffset;
			_timeLastGrounded = Time.time;
		}
		if (_spriteJumpsInScreenSpace)
		{
			_sprite.transform.position = basePosition + heightAboveGround * _sprite.transform.up;
		}
		else
		{
			_sprite.transform.position = transform.position;
		}

		// Prevent falling out of the world
		if (_rb.position.y < 0.0f)
		{
			_rb.position = new Vector3(_rb.position.x, 0.0f, _rb.position.z);
			_rb.velocity = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z);
			_timeLastGrounded = Time.time;
		}

		// Jump
		float timeSinceGrounded = Time.time - _timeLastGrounded;
		float timeSincePressedJump = Time.time - _timeLastPressedJump;
		float timeSinceJumped = Time.time - _timeLastJumped;
		if (timeSincePressedJump <= _jumpGracePeriod
			&& timeSinceGrounded <= _jumpGracePeriod
			&& timeSinceJumped >= _timeBetweenJumps)
		{
			_rb.velocity = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z);
			_rb.AddForce(_jumpForce * Vector3.up, ForceMode.VelocityChange);
			_timeLastJumped = Time.time;
		}

		// Animate
		UpdateAnimation();
	}

	private void MoveHorizontal()
	{
		// We want 'forward' and 'backward' to be up and down the screen, but since it's a perspective
		// camera we have to create a screenspace up vector then project it onto a flat plane
		Vector3 screenPos = _camera.WorldToScreenPoint(transform.position);
		Ray ray = _camera.ScreenPointToRay(screenPos);
		_groundPlane.Raycast(ray, out float entryDistance);
		Vector3 posA = ray.origin + entryDistance * ray.direction;

		ray = _camera.ScreenPointToRay(screenPos + Vector3.up);		// Screenspace up vector
		_groundPlane.Raycast(ray, out entryDistance);
		Vector3 posB = ray.origin + entryDistance * ray.direction;

		Vector3 forward = (posB - posA).normalized;		// Screenspace up vector projected to world space
		Vector3 right = _camera.transform.right;

		// Apply horizontal movement
		Vector2 input = _moveInput * _moveForce * Time.deltaTime;
		Vector3 movement = input.x * right + input.y * forward;
		_rb.AddForce(movement, ForceMode.VelocityChange);

		// Cap horizontal speed
		Vector3 velocity = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z);
		if (velocity.sqrMagnitude > _maxHorizontalSpeed * _maxHorizontalSpeed)
		{
			velocity = velocity.normalized * _maxHorizontalSpeed;
			_rb.velocity = new Vector3(velocity.x, _rb.velocity.y, velocity.z);
		}
	}

	private void UpdateAnimation()
	{
		// Anim
		Vector3 velocity = _rb.velocity;
		if ((Time.time - _timeLastGrounded) >= _jumpGracePeriod)
		{
			if (velocity.y > 0.0f)
			{
				_anim.PlayJump();
			}
			else
			{
				_anim.PlayFall();
			}
		}
		else
		{
			Vector3 horizontal = new Vector3(velocity.x, 0.0f, velocity.z);
			if (horizontal.sqrMagnitude >= _idleThreshold * _idleThreshold)
			{
				_anim.PlayWalk();
			}
			else
			{
				_anim.PlayIdle();
			}
		}

		// Sprite flipping
		if (_moveInput.x < -_spriteFlipThreshold)
		{
			_sprite.flipX = true;
		}
		else if (_moveInput.x > _spriteFlipThreshold)
		{
			_sprite.flipX = false;
		}
	}
}
