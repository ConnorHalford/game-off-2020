using UnityEngine;

public class Billboard : MonoBehaviour
{
	private Camera _camera = null;

	private void LateUpdate()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}
		if (_camera != null)
		{
			//Vector3 forward = transform.position - _camera.transform.position;
			//transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			transform.rotation = _camera.transform.rotation;
		}
	}
}
