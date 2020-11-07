using UnityEngine;

public static class Globals
{
	public static int MaskEnvironment { get { return _maskEnvironment; } }
	public static Controls Controls { get { return _controls; } }
	public static Camera Camera { get { return _camera; } }

	private static int _maskEnvironment = 0;
	private static Controls _controls = null;
	private static Camera _camera = null;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		Application.targetFrameRate = 60;
		_maskEnvironment = LayerMask.GetMask("Environment");
		_camera = Camera.main;
		_controls = new Controls();
		_controls.Enable();
	}
}
