using UnityEngine;

public class BlobShadow : MonoBehaviour
{
	[SerializeField] private Transform _follow = null;
	[SerializeField] private float _height = 2.0f;
	[SerializeField] private float _xOffset = 0.0f;
	[SerializeField] private float _zOffset = 0.0f;

	private void LateUpdate()
	{
		if (_follow == null)
		{
			return;
		}

		float y = _height;
		if (Physics.Raycast(_follow.position + _height * Vector3.up, Vector3.down, out RaycastHit hit,
			float.PositiveInfinity, Globals.MaskEnvironment))
		{
			y = hit.point.y + _height;
		}
		transform.position = new Vector3(_follow.position.x + _xOffset, y, _follow.position.z + _zOffset);
	}
}
