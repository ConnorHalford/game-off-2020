using UnityEngine;
using UnityEngine.UI;

public class UISpacecraft : MonoBehaviour
{
	[SerializeField] private RawImage _imageCraft = null;
	[SerializeField] private Image _imageOwner = null;
	[SerializeField] private Image _imageTimerFront = null;
	[SerializeField] private Sprite[] _ownerSprites = null;

	private RectTransform _rt = null;
	public RectTransform RectTransform { get { return _rt; } }

	private void Awake()
	{
		_rt = transform as RectTransform;
	}

	private void OnDestroy()
	{
		if (_imageCraft.texture != null)
		{
			Destroy(_imageCraft.texture);
		}
	}

	public void Populate(Spacecraft craft)
	{
		_imageCraft.texture = craft.CreatePreview();
		_imageOwner.sprite = _ownerSprites[(int)craft.Owner];
	}
}
