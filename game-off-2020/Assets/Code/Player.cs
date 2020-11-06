using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
	[SerializeField] private SpriteRenderer _sprite = null;
	[SerializeField] private float _moveSpeed = 3.0f;

	private Controls _controls = null;
	private Camera _camera = null;

	private void Awake()
	{
		_controls = new Controls();
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
		if (_camera == null)
		{
			_camera = Camera.main;
		}

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

		Vector2 input = _controls.Character.Movement.ReadValue<Vector2>() * _moveSpeed * Time.deltaTime;
		Vector3 movement = input.x * right + input.y * forward;
		transform.position += movement;
	}
}
