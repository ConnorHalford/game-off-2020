using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	private class QueuedSpacecraftUI
	{
		public Spacecraft Craft = null;
		public float CraftTimerPercentage = 1.0f;
	}

	private class SpacecraftUIAnim
	{
		public UISpacecraft UI = null;
		public Spacecraft Craft = null;
		public float AnimPercentage = 0.0f;
		public bool Arrival = true;
		public bool Appearing = true;
	}

	[Header("Game")]
	[SerializeField] private GameObject _rootGame = null;
	[SerializeField] private TextMeshProUGUI _textArrivals = null;
	[SerializeField] private TextMeshProUGUI _textDepartures = null;
	[SerializeField] private TextMeshProUGUI _textHUD = null;
	[SerializeField] private UISpacecraft _prefabUISpacecraft = null;
	[SerializeField] private TextMeshProUGUI _prefabTextCredits = null;
	[SerializeField] private float _spacing = 10.0f;
	[SerializeField] private float _animDuration = 0.3f;
	[SerializeField] private float _creditsMoveSpeed = 50.0f;
	[SerializeField] private float _creditsDuration = 0.5f;

	[Header("Menu")]
	[SerializeField] private GameObject _rootMenu = null;

	[Header("End")]
	[SerializeField] private GameObject _rootEnd = null;
	[SerializeField] private TextMeshProUGUI _textEnd = null;

	private UISpacecraft[] _arrivals = null;
	private UISpacecraft[] _departures = null;
	private List<QueuedSpacecraftUI> _queuedArrivals = null;
	private List<QueuedSpacecraftUI> _queuedDepartures = null;
	private List<SpacecraftUIAnim> _anims = null;
	private int _numVisibleArrivals = 0;
	private int _numVisibleDepartures = 0;

	private TextMeshProUGUI[] _credits = null;
	private float[] _creditsStartTime = null;

	private void Awake()
	{
		Globals.RegisterUIManager(this);
	}

	private void Start()
	{
		int maxArrivals = Globals.Game.MaxQueuedArrivals;
		CreatePool(_prefabUISpacecraft, ref _arrivals, maxArrivals, "Arrivals");
		_queuedArrivals = new List<QueuedSpacecraftUI>(maxArrivals);

		int maxDepartures = Globals.Game.MaxQueuedDepartures;
		CreatePool(_prefabUISpacecraft, ref _departures, maxDepartures, "Departures");
		_queuedDepartures = new List<QueuedSpacecraftUI>(maxDepartures);

		int maxCredits = 2 * (maxArrivals + maxDepartures);
		CreatePool(_prefabTextCredits, ref _credits, maxCredits, "CreditsText");
		_creditsStartTime = new float[maxCredits];
		for (int i = 0; i < maxCredits; ++i)
		{
			_creditsStartTime[i] = -1.0f;
		}

		_anims = new List<SpacecraftUIAnim>(maxArrivals + maxDepartures);

		Globals.Game.OnGameStateChanged += OnGameStateChanged;
		OnGameStateChanged(Globals.Game.State);
	}

	private void OnGameStateChanged(GameState state)
	{
		_rootMenu.SetActive(state == GameState.Menu);
		_rootGame.SetActive(state == GameState.Game);
		_rootEnd.SetActive(state == GameState.End);

		// Clear game UI
		_anims.Clear();
		_queuedArrivals.Clear();
		_queuedDepartures.Clear();
		_numVisibleArrivals = 0;
		_numVisibleDepartures = 0;
		int count = _arrivals.Length;
		for (int i = 0; i < count; ++i)
		{
			_arrivals[i].gameObject.SetActive(false);
		}
		count = _departures.Length;
		for (int i = 0; i < count; ++i)
		{
			_departures[i].gameObject.SetActive(false);
		}
		count = _credits.Length;
		for (int i = 0; i < count; ++i)
		{
			_credits[i].gameObject.SetActive(false);
		}

		if (state == GameState.End)
		{
			int numParked = Globals.Game.NumCraftParked;
			int numReturned = Globals.Game.NumCraftReturned;
			ulong credits = Globals.Game.Credits;
			_textEnd.text =
				"Your shift is finally over!"
				+ "\n"
				+ "\nYou parked " + numParked.ToString() + " spacecraft"
				+ "\nYou returned " + numReturned.ToString() + " spacecraft"
				+ "\nYou earned Ͼ" + credits.ToString("N0") + " space credits"
				+ "\n"
				+ "\nTime to go home, rest up, and do it all again tomorrow."
				+ "\nHooray for space capitalism!"
				+ "\n"
				+ "\n<align=center><b>Press the jump button to reflect on life as a...</b></align>";
		}
	}

	private void CreatePool<T>(T prefab, ref T[] array, int count, string parentName) where T : UnityEngine.Component
	{
		GameObject parent = new GameObject(parentName);
		parent.transform.SetParent(transform);
		array = new T[count];
		for (int i = 0; i < count; ++i)
		{
			array[i] = Instantiate(prefab, parent.transform);
			array[i].gameObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		if (Globals.Game.State != GameState.Game)
		{
			return;
		}

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
			QueuedSpacecraftUI queued = _queuedArrivals[0];
			_queuedArrivals.RemoveAt(0);
			StartFlight(queued.Craft, arrival: true, queued.CraftTimerPercentage);
		}
		while (_numVisibleDepartures < _departures.Length && _queuedDepartures.Count > 0)
		{
			QueuedSpacecraftUI queued = _queuedDepartures[0];
			_queuedDepartures.RemoveAt(0);
			StartFlight(queued.Craft, arrival: false, queued.CraftTimerPercentage);
		}

		// HUD
		ulong credits = Globals.Game.Credits;
		float secondsRemaining = Globals.Game.SecondsRemaining;
		int minutes = Mathf.FloorToInt(secondsRemaining / 60.0f);
		int seconds = Mathf.FloorToInt(secondsRemaining % 60);
		_textHUD.text = "Credits: Ͼ" + credits.ToString("N0")
			+ "\nShift remaining: " + minutes.ToString("D2") + ":" + seconds.ToString("D2");

		// Credits text
		count = _credits.Length;
		Vector3 deltaPos = Vector3.up * _creditsMoveSpeed * Time.deltaTime;
		for (int i = 0; i < count; ++i)
		{
			if (_creditsStartTime[i] >= 0.0f)
			{
				TextMeshProUGUI text = _credits[i];
				text.transform.position += deltaPos;
				float duration = Time.time - _creditsStartTime[i];
				text.alpha = Mathf.Clamp01(1.0f - duration / _creditsDuration);
				if (text.alpha <= 0.0f)
				{
					text.gameObject.SetActive(false);
					_creditsStartTime[i] = -1.0f;
				}
			}
		}
	}

	private void ApplyAnim(SpacecraftUIAnim anim)
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

	public void AddCredits(int credits, Vector3 position)
	{
		int count = _credits.Length;
		for (int i = 0; i < count; ++i)
		{
			if (!_credits[i].gameObject.activeInHierarchy)
			{
				TextMeshProUGUI text = _credits[i];
				text.gameObject.SetActive(true);
				text.transform.position = Globals.Camera.WorldToScreenPoint(position);
				text.alpha = 1.0f;
				text.text = "+Ͼ" + credits.ToString("N0");
				_creditsStartTime[i] = Time.time;
				break;
			}
		}
	}

	public void StartFlight(Spacecraft craft, bool arrival, float timerPercentage = 1.0f)
	{
		// Grab from pool
		int index = -1;
		UISpacecraft[] array = arrival ? _arrivals : _departures;
		List<QueuedSpacecraftUI> queue = arrival ? _queuedArrivals : _queuedDepartures;
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
			queue.Add(new QueuedSpacecraftUI() { Craft = craft, CraftTimerPercentage = 1.0f });
			return;
		}

		// Setup
		array[index].gameObject.SetActive(true);
		array[index].Populate(craft);
		_anims.Add(new SpacecraftUIAnim() { UI = array[index], Craft = craft, AnimPercentage = 0.0f, Arrival = arrival, Appearing = true });
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
					_anims.Add(new SpacecraftUIAnim() { UI = array[i], Craft = craft, AnimPercentage = 0.0f, Arrival = arrival, Appearing = false });
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
			List<QueuedSpacecraftUI> queue = arrival ? _queuedArrivals : _queuedDepartures;
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
