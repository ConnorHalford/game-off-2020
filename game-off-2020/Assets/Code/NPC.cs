using UnityEngine;

public class NPC : MonoBehaviour
{
	[SerializeField] private CharacterAnimator _anim = null;

	private Vector3 _moveFrom = Vector3.zero;
	private Vector3 _moveTo = Vector3.zero;
	private Transform _arcTarget = null;
	private Vector3 _arcOffset = Vector3.zero;
	private float _moveTimer = -1.0f;
	private float _moveDuration = 0.0f;
	private float _alphaStart = 1.0f;
	private float _alphaEnd = 1.0f;
	private bool _linear = true;
	private Vector3 _arcCenter = Vector3.zero;
	private System.Action _onMovementFinished = null;

	public void SetCharacter(CharacterAnimator.CharacterSelection character)
	{
		_anim.SetCharacter(character);
	}

	public void Show()
	{
		_anim.gameObject.SetActive(true);
	}

	public void Hide()
	{
		_anim.gameObject.SetActive(false);
	}

	public void MoveArc(Vector3 from, Transform target, Vector3 offset, float duration, bool faceRight, System.Action onFinished)
	{
		Show();
		_moveFrom = from;
		_arcTarget = target;
		if (_arcTarget == null)
		{
			_moveTo = offset;
		}
		_arcOffset = offset;
		UpdateArcDestination();
		_anim.SetAlpha(1.0f);
		_anim.SetFlipX(!faceRight);
		_moveTimer = 0.0f;
		_moveDuration = duration;
		_onMovementFinished = onFinished;
		_linear = false;
	}

	private void UpdateArcDestination()
	{
		if (_arcTarget != null)
		{
			_moveTo = _arcTarget.transform.position + _arcOffset;
		}
		_arcCenter = 0.5f * (_moveFrom + _moveTo);
		_arcCenter += Vector3.down;
	}

	public void MoveLinear(Vector3 from, Vector3 to, float duration, float startAlpha, float endAlpha, bool faceRight, System.Action onFinished)
	{
		Show();
		_arcTarget = null;
		_anim.PlayWalk();
		_moveFrom = from;
		_moveTo = to;
		_alphaStart = startAlpha;
		_alphaEnd = endAlpha;
		_anim.SetFlipX(!faceRight);
		_moveTimer = 0.0f;
		_moveDuration = duration;
		_onMovementFinished = onFinished;
		_linear = true;
	}

	private void LateUpdate()
	{
		if (_linear && _moveTimer < 0.0f)
		{
			transform.position = _moveTo;
		}
	}

	private void Update()
	{
		if (_moveTimer < 0.0f)
		{
			return;
		}

		_moveTimer += Time.deltaTime;
		float t = Mathf.Clamp01(_moveTimer / _moveDuration);
		if (_linear)
		{
			transform.position = Vector3.Lerp(_moveFrom, _moveTo, t);
			_anim.SetAlpha(Mathf.Lerp(_alphaStart, _alphaEnd, t));
		}
		else
		{
			UpdateArcDestination();
			transform.position = _arcCenter + Vector3.Slerp(_moveFrom - _arcCenter, _moveTo - _arcCenter, t);
		}

		if (_moveTimer >= _moveDuration)
		{
			_moveTimer = -1.0f;
			_anim.PlayIdle();
			if (_onMovementFinished != null)
			{
				_onMovementFinished();
			}
		}
	}
}
