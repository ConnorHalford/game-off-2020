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

	private UISpacecraft[] _arrivals = null;
	private UISpacecraft[] _departures = null;

	private void Awake()
	{
		Globals.RegisterUIManager(this);
	}

	private void Start()
	{
		CreatePool(ref _arrivals, Globals.Game.MaxQueuedArrivals, "Arrivals");
		CreatePool(ref _departures, Globals.Game.MaxQueuedDepartures, "Departures");
	}

	private void CreatePool(ref UISpacecraft[] array, int count, string parentName)
	{
		GameObject parent = new GameObject(parentName);
		parent.transform.SetParent(transform);
		array = new UISpacecraft[count];
		for (int i = 0; i < count; ++i)
		{
			array[i] = Instantiate(PrefabUISpacecraft, parent.transform);
			array[i].gameObject.SetActive(false);
		}
	}

	public void AddSpacecraft(Spacecraft craft, bool arrival)
	{
		// Grab from pool
		UISpacecraft[] array = arrival ? _arrivals : _departures;
		int count = array.Length;
		int index = -1;
		for (int i = 0; i < count; ++i)
		{
			if (!array[i].gameObject.activeInHierarchy)
			{
				index = i;
				break;
			}
		}
		Debug.Assert(index >= 0);

		// Setup
		array[index].gameObject.SetActive(true);
		array[index].Populate(craft);

		// Position
		RectTransform root = arrival ? _textArrivals.rectTransform : _textDepartures.rectTransform;
		Vector3 position = Vector3.zero;
		if (!arrival)
		{
			position.x = Screen.width - array[index].RectTransform.sizeDelta.x;
		}
		if (index == 0)
		{
			position.y = BottomY(root) - _spacing;
		}
		else
		{
			position.y = BottomY(array[index - 1].RectTransform) - _spacing;
		}
		array[index].RectTransform.position = position;
	}

	public void RemoveSpacecraft(Spacecraft craft, bool arrival)
	{
		UISpacecraft[] array = arrival ? _arrivals : _departures;
		int count = array.Length;
		for (int i = 0; i < count; ++i)
		{
			if (array[i].Craft == craft)
			{
				array[i].gameObject.SetActive(false);
				break;
			}
		}
	}

	public void UpdateSpacecraft(Spacecraft craft, float timerPercentage, bool arrival)
	{
		UISpacecraft[] array = arrival ? _arrivals : _departures;
		int count = array.Length;
		for (int i = 0; i < count; ++i)
		{
			if (array[i].Craft == craft)
			{
				array[i].SetTimerPercentage(timerPercentage);
				break;
			}
		}
	}

	private static float BottomY(RectTransform rt)
	{
		return rt.TransformPoint(rt.rect.min).y;
	}
}
