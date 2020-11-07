using UnityEngine;

public class Billboard : MonoBehaviour
{
	private void LateUpdate()
	{
		transform.rotation = Globals.Camera.transform.rotation;
	}
}
