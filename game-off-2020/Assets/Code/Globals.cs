using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
	private static int _maskEnvironment = 0;
	private static int _maskSpacecraft = 0;
	private static Controls _controls = null;

	private static Camera _camera = null;
	private static Player _player = null;
	private static Spacecraft _driving = null;
	private static List<Spacecraft> _spacecraft = new List<Spacecraft>();

	private static UIManager _uiManager = null;
	private static Game _game = null;

	public static int MaskEnvironment { get { return _maskEnvironment; } }
	public static int MaskSpacecraft { get { return _maskSpacecraft; } }
	public static Controls Controls { get { return _controls; } }

	public static Camera Camera { get { return _camera; } }
	public static Player Player { get { return _player; } }
	public static Spacecraft Driving { get { return _driving; } }
	public static bool IsDriving { get { return _driving != null; } }

	public static UIManager UIManager { get { return _uiManager; } }
	public static Game Game { get { return _game; } }

	public static event System.Action<Spacecraft> OnStartDriving = null;
	public static event System.Action<Spacecraft> OnStopDriving = null;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		Application.targetFrameRate = 60;
		_maskEnvironment = LayerMask.GetMask("Environment");
		_maskSpacecraft = LayerMask.GetMask("Spacecraft");
		_camera = Camera.main;
		_controls = new Controls();
		_controls.Enable();
	}

	public static void RegisterPlayer(Player player)
	{
		_player = player;
	}

	public static void StartDriving(Spacecraft craft)
	{
		_driving = craft;
		if (OnStartDriving != null)
		{
			OnStartDriving(craft);
		}
	}

	public static void StopDriving()
	{
		Spacecraft exiting = _driving;
		_driving = null;
		if (OnStopDriving != null)
		{
			OnStopDriving(exiting);
		}
	}

	public static void RegisterUIManager(UIManager manager)
	{
		_uiManager = manager;
	}

	public static void RegisterGame(Game game)
	{
		_game = game;
	}
}
