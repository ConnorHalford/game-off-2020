using UnityEngine;

public class Spacecraft : MonoBehaviour
{
	public enum Model
	{
		CargoA,
		CargoB,
		Miner,
		Racer,
		SpeederA,
		SpeederB,
		SpeederC,
		SpeederD
	}

#if UNITY_EDITOR
	private class DebugRaycastData
	{
		public Vector3 Start = Vector3.zero;
		public Vector3 End = Vector3.zero;
		public Vector3 Highlight = Vector3.zero;

		public void Set(Vector3 start, Vector3 end, Vector3 highlight)
		{
			Start = start;
			End = end;
			Highlight = highlight;
		}
	}
	private DebugRaycastData _debugRaycastFront = new DebugRaycastData();
	private DebugRaycastData _debugRaycastCenter = new DebugRaycastData();
	private DebugRaycastData _debugRaycastRear = new DebugRaycastData();

	[Header("Debug")]
	[SerializeField] private float _debugRadius = 0.1f;
	[SerializeField] private Color _debugLineColor = Color.white;
	[SerializeField] private Color _debugSphereColor = Color.white;
	[SerializeField] private Color _debugHighlightColor = Color.yellow;
#endif	// UNITY_EDITOR

	[Header("Model")]
	[SerializeField] private MeshCollider[] _models = new MeshCollider[System.Enum.GetValues(typeof(Model)).Length];
	[SerializeField] private Transform _modelRoot = null;
	[SerializeField] private Color[] _colors = null;
	[SerializeField] private Camera _previewCamera = null;

	[Header("Owner")]
	[SerializeField] private NPC _npc = null;

	[Header("Height")]
	[SerializeField] private Transform _frontLeftAnchor = null;
	[SerializeField] private Transform _frontRightAnchor = null;
	[SerializeField] private Transform _rearLeftAnchor = null;
	[SerializeField] private Transform _rearRightAnchor = null;
	[SerializeField] private float _heightWhenDriving = 0.5f;
	[SerializeField] private float _heightWhenParked = 0.1f;
	[SerializeField] private float _levitationSpeedUp = 5.0f;
	[SerializeField] private float _levitationSpeedDown = 1.0f;
	[SerializeField, Range(0.0f, 1.0f)] private float _normalThreshold = 0.8f;
	[SerializeField, Range(0.0f, 1.0f)] private float _heightThreshold = 0.8f;

	[Header("Turning")]
	[SerializeField] private float _turnSpeed = 5.0f;
	[SerializeField] private float _rollAmount = 1.0f;

	[Header("Driving")]
	[SerializeField] private float _driveForceForward = 60.0f;
	[SerializeField] private float _driveForceReverse = 30.0f;
	[SerializeField] private float _maxForwardSpeed = 10.0f;
	[SerializeField] private float _maxReverseSpeed = 1.0f;
	[SerializeField] private float _minTimeBeforeExit = 0.1f;

	[Header("Outline")]
	[SerializeField] private MeshFilter _outline = null;
	[SerializeField] private float _outlineScale = 1.1f;

	private Rigidbody _rb = null;
	private float _timeStartedDriving = -1.0f;
	private float _targetHeight = 0.0f;
	private Vector2 _moveInput = Vector2.zero;

	private Model _model = Model.CargoA;
	private Model _modelPrevFrame = Model.CargoA;
	private CharacterAnimator.CharacterSelection _owner = CharacterAnimator.CharacterSelection.Beige;
	private Mesh _outlineMesh = null;
	private int _colorIndex = -1;

	public bool IsDriving { get { return _timeStartedDriving >= 0.0f; } }
	public CharacterAnimator.CharacterSelection Owner { get { return _owner; } }
	public float HeightWhenDriving { get { return _heightWhenDriving; } }
	public NPC NPC { get { return _npc; } }

	private void Start()
	{
		Globals.Game.OnGameStateChanged += OnGameStateChanged;
	}

	private void OnGameStateChanged(GameState state)
	{
		_rb.velocity = Vector3.zero;
		_rb.angularVelocity = Vector3.zero;
	}

	public void Spawn()
	{
		_rb = GetComponent<Rigidbody>();
		_rb.velocity = Vector3.zero;
		_rb.angularVelocity = Vector3.zero;
		_model = (Model)Random.Range(0, System.Enum.GetValues(typeof(Model)).Length);
		ChangeModel(_model);
		SetOutlineVisible(false);
		_owner = CharacterAnimator.GetRandomNPC();
		_npc.SetCharacter(_owner);
		_npc.Hide();
	}

	public void ResetModelRoll()
	{
		Quaternion rot = Quaternion.LookRotation(_modelRoot.forward, Vector3.up);
		_rb.rotation = rot;
		_rb.angularVelocity = Vector3.zero;
		_modelRoot.rotation = rot;
	}

	public bool IsEquivalent(Spacecraft other)
	{
		return this._model == other._model
			&& this._owner == other._owner
			&& this._colorIndex == other._colorIndex;
	}

	private void OnDestroy()
	{
		Destroy(_outlineMesh);
	}

	private void Update()
	{
		if (Globals.Game.State != GameState.Game)
		{
			return;
		}

		// Change model
		if (_model != _modelPrevFrame)
		{
			ChangeModel(_model);
		}

		_moveInput = Vector2.zero;
		if (IsDriving)
		{
			// Stop driving
			float timeSpentDriving = Time.time - _timeStartedDriving;
			if (timeSpentDriving >= _minTimeBeforeExit && Globals.Controls.Character.EnterSpacecraft.triggered)
			{
				StopDriving();
			}
			else
			{
				// Store move input
				_moveInput = Globals.Controls.Character.Movement.ReadValue<Vector2>();
			}
		}
	}

	private void LateUpdate()
	{
		if (Globals.Game.State != GameState.Game)
		{
			return;
		}
		ResetUnwantedRotations();
	}

	private Vector3 CalcGroundPoint(Vector3 position)
	{
		Vector3 groundPoint = new Vector3(position.x, 0.0f, position.z);
		if (Physics.Raycast(position, Vector3.up, out RaycastHit hit, float.MaxValue, Globals.MaskEnvironment))
		{
			groundPoint = hit.point;
		}
		else if (Physics.Raycast(position, Vector3.down, out hit, float.MaxValue, Globals.MaskEnvironment))
		{
			groundPoint = hit.point;
			if (Vector3.Dot(hit.normal, Vector3.up) < _normalThreshold)
			{
				groundPoint = new Vector3(groundPoint.x, Mathf.Ceil(groundPoint.y), groundPoint.z);
			}
		}
		return groundPoint;
	}

	private void FixedUpdate()
	{
		if (Globals.Game.State != GameState.Game)
		{
			return;
		}

		// Levitate
		Vector3 frontAnchor = 0.5f * (_frontLeftAnchor.position + _frontRightAnchor.position);
		Vector3 rearAnchor = 0.5f * (_rearLeftAnchor.position + _rearRightAnchor.position);
		Vector3 frontGround = CalcGroundPoint(frontAnchor);
		Vector3 rearGround = CalcGroundPoint(rearAnchor);
		Vector3 centerGround = CalcGroundPoint(_rb.position);

		if (Mathf.Max(Mathf.Abs(frontGround.y - centerGround.y), Mathf.Abs(centerGround.y - rearGround.y)) >= _heightThreshold)
		{
			float biggestY = Mathf.Max(frontGround.y, centerGround.y, rearGround.y);
			frontGround = new Vector3(frontGround.x, biggestY, frontGround.z);
			rearGround = new Vector3(rearGround.x, biggestY, rearGround.z);
			centerGround = new Vector3(centerGround.x, biggestY, centerGround.z);
		}

		Vector3 frontTarget = frontGround + _targetHeight * Vector3.up;
		Vector3 rearTarget = rearGround + _targetHeight * Vector3.up;
		Vector3 rearToFront = frontTarget - rearTarget;
		float targetY = Mathf.Max((rearTarget + 0.5f * rearToFront).y, centerGround.y + _targetHeight);
		float y = _rb.position.y;
		float deltaY = Time.deltaTime * (y < targetY ? _levitationSpeedUp : _levitationSpeedDown);
		y = Mathf.SmoothStep(y, targetY, deltaY);
		_rb.position = new Vector3(_rb.position.x, y, _rb.position.z);

#if UNITY_EDITOR
		_debugRaycastFront.Set(frontAnchor, frontGround, frontTarget);
		_debugRaycastRear.Set(rearAnchor, rearGround, rearTarget);
		_debugRaycastCenter.Set(0.5f * (frontAnchor + rearAnchor), centerGround, _rb.position);
#endif	// UNITY_EDITOR

		// Rotate
		if (IsDriving)
		{
			float turn = _moveInput.x * _turnSpeed * Time.deltaTime;
			_rb.angularVelocity += turn * Vector3.up;
		}
		Vector3 pitchAndYaw = Quaternion.LookRotation(rearToFront, Vector3.up).eulerAngles;
		float roll = _rb.angularVelocity.y * _rollAmount * Time.deltaTime;
		Quaternion modelRotation = Quaternion.Euler(pitchAndYaw.x, pitchAndYaw.y, roll);
		_modelRoot.rotation = modelRotation;

		// Drive
		if (IsDriving)
		{
			float force = (_moveInput.y > 0.0f ? _driveForceForward : _driveForceReverse) * _moveInput.y * Time.deltaTime;
			Vector3 direction = new Vector3(_modelRoot.forward.x, 0.0f, _modelRoot.forward.z);	// note not normalized when not horizontal
			_rb.AddForce(force * direction, ForceMode.VelocityChange);
		}

		// Cap horizontal speed
		Vector3 velocity = new Vector3(_rb.velocity.x, 0.0f, _rb.velocity.z);
		bool movingForward = Vector3.Dot(velocity, transform.forward) > 0.0f;
		float maxSpeed = movingForward ? _maxForwardSpeed : _maxReverseSpeed;
		if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
		{
			velocity = velocity.normalized * maxSpeed;
			_rb.velocity = new Vector3(velocity.x, _rb.velocity.y, velocity.z);
		}

		ResetUnwantedRotations();
	}

	private void ResetUnwantedRotations()
	{
		Vector3 rotation = transform.eulerAngles;
		if (_rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX))
		{
			rotation = new Vector3(0.0f, rotation.y, rotation.z);
		}
		if (_rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY))
		{
			rotation = new Vector3(rotation.x, 0.0f, rotation.z);
		}
		if (_rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationZ))
		{
			rotation = new Vector3(rotation.x, rotation.y, 0.0f);
		}
		transform.eulerAngles = rotation;
	}

	public void StartDriving()
	{
		_targetHeight = _heightWhenDriving;
		_timeStartedDriving = Time.time;
		Globals.StartDriving(this);
	}

	private void StopDriving()
	{
		_targetHeight = _heightWhenParked;
		_timeStartedDriving = -1.0f;
		Globals.StopDriving();
	}

	public void SetOutlineVisible(bool visible)
	{
		_outline.gameObject.SetActive(visible);
	}

	private void ChangeModel(Model model)
	{
		// Enable correct model
		_model = model;
		_modelPrevFrame = _model;
		_models[(int)Model.CargoA].gameObject.SetActive(_model == Model.CargoA);
		_models[(int)Model.CargoB].gameObject.SetActive(_model == Model.CargoB);
		_models[(int)Model.Miner].gameObject.SetActive(_model == Model.Miner);
		_models[(int)Model.Racer].gameObject.SetActive(_model == Model.Racer);
		_models[(int)Model.SpeederA].gameObject.SetActive(_model == Model.SpeederA);
		_models[(int)Model.SpeederB].gameObject.SetActive(_model == Model.SpeederB);
		_models[(int)Model.SpeederC].gameObject.SetActive(_model == Model.SpeederC);
		_models[(int)Model.SpeederD].gameObject.SetActive(_model == Model.SpeederD);

		// Tint model to random color
		MeshRenderer renderer = _models[(int)_model].GetComponent<MeshRenderer>();
		Material[] sharedMaterials = renderer.sharedMaterials;
		int numSharedMaterials = sharedMaterials.Length;
		int tintMaterialIndex = -1;
		for (int i = 0; i < numSharedMaterials; ++i)
		{
			if (string.Equals(sharedMaterials[i].name, "metalRed", System.StringComparison.Ordinal))
			{
				tintMaterialIndex = i;
				break;
			}
		}
		Debug.Assert(tintMaterialIndex >= 0);
		_colorIndex = Random.Range(0, _colors.Length);
		Color tint = _colors[_colorIndex];
		tint = new Color(tint.r, tint.g, tint.b, 1.0f);
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		renderer.GetPropertyBlock(block, tintMaterialIndex);
		block.SetColor("_Color", tint);
		renderer.SetPropertyBlock(block, tintMaterialIndex);

		// Generate inverted hull outline mesh
		if (_outlineMesh != null)
		{
			Destroy(_outlineMesh);
			_outlineMesh = null;
		}
		Mesh modelMesh = _models[(int)_model].GetComponent<MeshFilter>().sharedMesh;
		_outlineMesh = new Mesh();
		int numTris = modelMesh.triangles.Length;
		int[] invertedTris = new int[numTris];
		for (int i = 0; i < numTris; i += 3)
		{
			invertedTris[i] = modelMesh.triangles[i];
			invertedTris[i + 1] = modelMesh.triangles[i + 2];	// Swap winding
			invertedTris[i + 2] = modelMesh.triangles[i + 1];
		}
		_outlineMesh.vertices = modelMesh.vertices;
		_outlineMesh.normals = modelMesh.normals;
		_outlineMesh.triangles = invertedTris;
		_outline.sharedMesh = _outlineMesh;
		_outline.transform.localScale = _outlineScale * Vector3.one;

		DisableCollider();
	}

	public void EnableCollider()
	{
		_models[(int)_model].enabled = true;
	}

	public void DisableCollider()
	{
		_models[(int)_model].enabled = false;
	}

	public Texture2D CreatePreview()
	{
		// Setup
		int resolution = 256;
		RenderTexture renderTex = RenderTexture.GetTemporary(resolution, resolution, depthBuffer: 24,
			RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, antiAliasing: 2);
		RenderTexture prevActive = RenderTexture.active;
		RenderTexture.active = renderTex;
		_previewCamera.gameObject.SetActive(true);
		_previewCamera.aspect = 1.0f;
		_previewCamera.targetTexture = renderTex;
		bool enableOutline = _outline.gameObject.activeInHierarchy;
		if (enableOutline)
		{
			_outline.gameObject.SetActive(false);
		}
		Vector3 modelPosition = _modelRoot.transform.position;
		_modelRoot.transform.position = 200.0f * Vector3.down;

		// Render
		_previewCamera.Render();
		Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, mipChain: false);
		tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
		tex.Apply();

		// Cleanup
		_previewCamera.targetTexture = null;
		_previewCamera.gameObject.SetActive(false);
		RenderTexture.active = prevActive;
		RenderTexture.ReleaseTemporary(renderTex);
		if (enableOutline)
		{
			_outline.gameObject.SetActive(true);
		}
		_modelRoot.transform.position = modelPosition;

		return tex;
	}

#if UNITY_EDITOR
	private void DrawDebugRaycast(DebugRaycastData raycast)
	{
		Gizmos.color = _debugSphereColor;
		Gizmos.DrawSphere(raycast.Start, _debugRadius);
		Gizmos.DrawSphere(raycast.End, _debugRadius);
		Gizmos.color = _debugLineColor;
		Gizmos.DrawLine(raycast.Start, raycast.End);
		Gizmos.DrawLine(raycast.Start, raycast.Highlight);
		Gizmos.DrawLine(raycast.Highlight, raycast.End);
		Gizmos.color = _debugHighlightColor;
		Gizmos.DrawSphere(raycast.Highlight, _debugRadius);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = _debugSphereColor;
		Gizmos.DrawSphere(_frontLeftAnchor.position, _debugRadius);
		Gizmos.DrawSphere(_frontRightAnchor.position, _debugRadius);
		Gizmos.DrawSphere(_rearLeftAnchor.position, _debugRadius);
		Gizmos.DrawSphere(_rearRightAnchor.position, _debugRadius);

		Gizmos.color = _debugLineColor;
		Gizmos.DrawLine(_frontLeftAnchor.position, _frontRightAnchor.position);
		Gizmos.DrawLine(_frontRightAnchor.position, _rearRightAnchor.position);
		Gizmos.DrawLine(_rearRightAnchor.position, _rearLeftAnchor.position);
		Gizmos.DrawLine(_rearLeftAnchor.position, _frontLeftAnchor.position);

		DrawDebugRaycast(_debugRaycastFront);
		DrawDebugRaycast(_debugRaycastCenter);
		DrawDebugRaycast(_debugRaycastRear);
	}
#endif	// UNITY_EDITOR
}
