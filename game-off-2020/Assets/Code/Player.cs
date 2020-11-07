using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
	[SerializeField] private SpriteRenderer _sprite = null;
	[SerializeField] private float _moveForce = 30.0f;
	[SerializeField] private float _maxHorizontalSpeed = 10.0f;

	private Controls _controls = null;
	private Vector2 _moveInput = Vector2.zero;
	private Camera _camera = null;
	private Rigidbody _rb = null;
	private CapsuleCollider _collider = null;

	private int _layerMask = 0;

	private void Awake()
	{
		_controls = new Controls();
		_rb = GetComponent<Rigidbody>();
		_collider = GetComponent<CapsuleCollider>();
		_layerMask = LayerMask.GetMask("Environment");
	}

	private void OnEnable()
	{
		_controls.Character.Enable();
	}

	private void OnDisable()
	{
		_controls.Character.Disable();
	}

	private void Update()
	{
		_moveInput = _controls.Character.Movement.ReadValue<Vector2>();
	}

	private void FixedUpdate()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}

		MoveHorizontal();



		// Prevent falling out of the world
		if (_rb.position.y < 0.0f)
		{
			_rb.position = new Vector3(_rb.position.x, 0.0f, _rb.position.z);
			_rb.velocity = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z);
		}
	}

	private void MoveHorizontal()
	{
		// We want 'forward' and 'backward' to be up and down the screen, but since it's a perspective
		// camera we have to create a screenspace up vector then project it onto a flat plane
		Plane ground = new Plane(Vector3.up, transform.position);
		Vector3 screenPos = _camera.WorldToScreenPoint(transform.position);
		Ray ray = _camera.ScreenPointToRay(screenPos);
		ground.Raycast(ray, out float entryDistance);
		Vector3 posA = ray.origin + entryDistance * ray.direction;

		ray = _camera.ScreenPointToRay(screenPos + Vector3.up);		// Screenspace up vector
		ground.Raycast(ray, out entryDistance);
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
}
