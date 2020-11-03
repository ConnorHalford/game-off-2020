using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShadowSprite : MonoBehaviour
{
	[SerializeField] private Color _shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);

	private SpriteRenderer _baseRenderer = null;
	private SpriteRenderer _shadowRenderer = null;

	private static int _layerMask = 0;

	private void Awake()
	{
		_baseRenderer = GetComponent<SpriteRenderer>();

		GameObject shadowGO = new GameObject(name + " Shadow");
		shadowGO.transform.parent = transform;
		_shadowRenderer = shadowGO.AddComponent<SpriteRenderer>();
		_shadowRenderer.color = _shadowColor;
		_shadowRenderer.sortingOrder = _baseRenderer.sortingOrder - 1;

		_layerMask = LayerMask.GetMask("Environment");
	}

	private void LateUpdate()
	{
		if (_baseRenderer == null || _shadowRenderer == null)
		{
			return;
		}
		_shadowRenderer.sprite = _baseRenderer.sprite;

		Vector3 position = transform.position;
		if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _layerMask))
		{
			position = hit.point;
		}
		_shadowRenderer.transform.position = position + 0.01f * Vector3.up;

		Vector3 rotation = transform.rotation.eulerAngles;
		_shadowRenderer.transform.localEulerAngles = new Vector3(90.0f - rotation.x, 0.0f, 0.0f);
	}
}
