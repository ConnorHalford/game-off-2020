﻿using UnityEngine;

public class BlobShadow : MonoBehaviour
{
	[SerializeField] private Transform _follow = null;
	[SerializeField] private float _height = 2.0f;
	[SerializeField] private float _xOffset = 0.0f;
	[SerializeField] private float _zOffset = 0.0f;

	private static int _layerMask = 0;

	private void Awake()
	{
		_layerMask = LayerMask.GetMask("Environment");
	}

	private void LateUpdate()
	{
		if (_follow == null)
		{
			return;
		}

		float y = _height;
		if (Physics.Raycast(_follow.position, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _layerMask))
		{
			y = hit.point.y + _height;
		}
		transform.position = new Vector3(_follow.position.x + _xOffset, y, _follow.position.z + _zOffset);
	}
}