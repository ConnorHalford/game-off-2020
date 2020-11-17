using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public RawImage Spacecraft = null;

	private void Awake()
	{
		Globals.RegisterUIManager(this);
	}
}
