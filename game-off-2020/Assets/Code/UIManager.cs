using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	private class QueuedUI
	{
		public Spacecraft Craft = null;
		public float CraftTimerPercentage = 1.0f;
	}
	private class UIAnim
	{
		public UISpacecraft UI = null;
		public Spacecraft Craft = null;
		public float AnimPercentage = 0.0f;
		public bool Arrival = true;
		public bool Appearing = true;
	}

	[SerializeField] private TextMeshProUGUI _textArrivals = null;
	[SerializeField] private TextMeshProUGUI _textDepartures = null;
	[SerializeField] private UISpacecraft PrefabUISpacecraft = null;
	[SerializeField] private float _spacing = 10.0f;
	[SerializeField] private float _animDuration = 0.3f;

	private UISpacecraft[] _arrivals = null;
	private UISpacecraft[] _departures = null;
	private List<QueuedUI> _queuedArrivals = null;
	private List<QueuedUI> _queuedDepartures = null;
	private List<UIAnim> _anims = null;
	private int _numVisibleArrivals = 0;
	private int _numVisibleDepartures = 0;

	private void Awake()
	{
		Globals.RegisterUIManager(this);
	}

	private void Start()
	{
		int maxArrivals = Globals.Game.MaxQueuedArrivals;
		CreatePool(ref _arrivals, maxArrivals, "Arrivals");
		_queuedArrivals = new List<QueuedUI>(maxArrivals);

		int maxDepartures = Globals.Game.MaxQueuedDepartures;
		CreatePool(ref _departures, maxDepartures, "Departures");
		_queuedDepartures = new List<QueuedUI>(maxDepartures);

		_anims = new List<UIAnim>(maxArrivals + maxDepartures);
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

	private void LateUpdate()
	{
		// Update anims
		int count = _anims == null ? 0 : _anims.Count;
		float deltaAnim = Time.deltaTime / _animDuration;
		for (int i = count - 1; i >= 0; --i)
		{
			_anims[i].AnimPercentage = Mathf.Clamp01(_anims[i].AnimPercentage + deltaAnim);
			ApplyAnim(_anims[i]);

			if (_anims[i].AnimPercentage >= 1.0f)
			{
				// Finish
				if (!_anims[i].Appearing)
				{
					if (_anims[i].Arrival)
					{
						--_numVisibleArrivals;
					}
					else
					{
						--_numVisibleDepartures;
					}
					_anims[i].UI.gameObject.SetActive(false);
				}
				_anims.RemoveAt(i);
			}
		}

		// Pull from queue if any available
		while (_numVisibleArrivals < _arrivals.Length && _queuedArrivals.Count > 0)
		{
			QueuedUI queued = _queuedArrivals[0];
			_queuedArrivals.RemoveAt(0);
			AddSpacecraft(queued.Craft, arrival: true, queued.CraftTimerPercentage);
		}
		while (_numVisibleDepartures < _departures.Length && _queuedDepartures.Count > 0)
		{
			QueuedUI queued = _queuedDepartures[0];
			_queuedDepartures.RemoveAt(0);
			AddSpacecraft(queued.Craft, arrival: false, queued.CraftTimerPercentage);
		}
	}

	private void ApplyAnim(UIAnim anim)
	{
		float width = anim.UI.RectTransform.sizeDelta.x;
		float startX = -width, endX = 0.0f;
		if (!anim.Arrival)
		{
			startX = Screen.width;
			endX = Screen.width - width;
		}
		float alpha = anim.AnimPercentage;
		if (!anim.Appearing)
		{
			float temp = startX;	// Reverse to disappear
			startX = endX;
			endX = temp;
			alpha = 1.0f - alpha;
		}
		Vector3 pos = anim.UI.RectTransform.position;
		pos.x = Mathf.Lerp(startX, endX, anim.AnimPercentage);
		anim.UI.RectTransform.position = pos;
		anim.UI.SetAlphaMultiplier(alpha);
	}

	public void AddSpacecraft(Spacecraft craft, bool arrival, float timerPercentage = 1.0f)
	{
		// Grab from pool
		int index = -1;
		UISpacecraft[] array = arrival ? _arrivals : _departures;
		List<QueuedUI> queue = arrival ? _queuedArrivals : _queuedDepartures;
		if (queue.Count == 0)
		{
			int count = array.Length;
			for (int i = 0; i < count; ++i)
			{
				if (!array[i].gameObject.activeInHierarchy)
				{
					index = i;
					break;
				}
			}
		}
		if (index == -1)
		{
			// All occupied, add to queue
			queue.Add(new QueuedUI() { Craft = craft, CraftTimerPercentage = 1.0f });
			return;
		}

		// Setup
		array[index].gameObject.SetActive(true);
		array[index].Populate(craft);
		_anims.Add(new UIAnim() { UI = array[index], Craft = craft, AnimPercentage = 0.0f, Arrival = arrival, Appearing = true });
		ApplyAnim(_anims[_anims.Count - 1]);
		if (arrival)
		{
			++_numVisibleArrivals;
		}
		else
		{
			++_numVisibleDepartures;
		}

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
				// Reverse animation
				bool reversed = false;
				int numAnims = _anims.Count;
				for (int animIndex = 0; animIndex < numAnims; ++animIndex)
				{
					if (_anims[animIndex].Craft == craft)
					{
						_anims[animIndex].AnimPercentage = 1.0f - _anims[animIndex].AnimPercentage;
						_anims[animIndex].Appearing = false;
						reversed = true;
						break;
					}
				}
				if (!reversed)
				{
					// No existing anim playing so make new one
					_anims.Add(new UIAnim() { UI = array[i], Craft = craft, AnimPercentage = 0.0f, Arrival = arrival, Appearing = false });
					ApplyAnim(_anims[_anims.Count - 1]);
				}
				break;
			}
		}
	}

	public void UpdateSpacecraft(Spacecraft craft, float timerPercentage, bool arrival)
	{
		UISpacecraft[] array = arrival ? _arrivals : _departures;
		bool found = false;
		int count = array.Length;
		for (int i = 0; i < count; ++i)
		{
			if (array[i].Craft == craft)
			{
				array[i].SetTimerPercentage(timerPercentage);
				found = true;
				break;
			}
		}
		if (!found)
		{
			List<QueuedUI> queue = arrival ? _queuedArrivals : _queuedDepartures;
			count = queue.Count;
			for (int i = 0; i < count; ++i)
			{
				if (queue[i].Craft == craft)
				{
					queue[i].CraftTimerPercentage = timerPercentage;
					found = true;
					break;
				}
			}
		}
	}

	private static float BottomY(RectTransform rt)
	{
		return rt.TransformPoint(rt.rect.min).y;
	}
}
