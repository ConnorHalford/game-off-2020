using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _textArrivals = null;
	[SerializeField] private TextMeshProUGUI _textDepartures = null;
	[SerializeField] private UISpacecraft PrefabUISpacecraft = null;
	[SerializeField] private float _spacing = 10.0f;

	private List<UISpacecraft> _arrivals = new List<UISpacecraft>();
	private List<UISpacecraft> _departures = new List<UISpacecraft>();

	private void Awake()
	{
		Globals.RegisterUIManager(this);
	}

	public void OnRegisterSpacecraft(Spacecraft craft)
	{
		UISpacecraft ui = Instantiate(PrefabUISpacecraft, transform);
		ui.Populate(craft);

		Vector3 position = Vector3.zero;
		if (_arrivals.Count == 0)
		{
			position.y = BottomY(_textArrivals.rectTransform) - 0.5f * _spacing;
		}
		else
		{
			position.y = BottomY(_arrivals[_arrivals.Count - 1].RectTransform) - _spacing;
		}
		ui.RectTransform.position = position;

		_arrivals.Add(ui);
	}

	private static float BottomY(RectTransform rt)
	{
		return rt.TransformPoint(rt.rect.min).y;
	}
}
