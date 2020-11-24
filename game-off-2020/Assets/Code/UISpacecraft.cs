using UnityEngine;
using UnityEngine.UI;

public class UISpacecraft : MonoBehaviour
{
	[SerializeField] private CanvasGroup _group = null;
	[SerializeField] private RawImage _imageCraft = null;
	[SerializeField] private Image _imageOwner = null;
	[SerializeField] private Image _imageTimerFront = null;
	[SerializeField] private Sprite[] _ownerSprites = null;
	[SerializeField] private Gradient _timerGradient = null;

	private RectTransform _rt = null;
	private Spacecraft _craft = null;

	public RectTransform RectTransform { get { return _rt; } }
	public Spacecraft Craft { get { return _craft; } }

	private void Awake()
	{
		_rt = transform as RectTransform;
	}

	public void Populate(Spacecraft craft)
	{
		_craft = craft;
		_imageCraft.texture = craft.CreatePreview();
		_imageOwner.sprite = _ownerSprites[(int)craft.Owner];
		SetTimerPercentage(1.0f);
		SetAlphaMultiplier(1.0f);
		Resources.UnloadUnusedAssets();		// Flush old preview texture
	}

	public void SetTimerPercentage(float timerPercentage)
	{
		_imageTimerFront.transform.localScale = new Vector3(timerPercentage, 1.0f, 1.0f);
		_imageTimerFront.color = _timerGradient.Evaluate(timerPercentage);
	}

	public void SetAlphaMultiplier(float alpha)
	{
		_group.alpha = alpha;
	}
}
