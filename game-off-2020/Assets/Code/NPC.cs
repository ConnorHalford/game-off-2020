using UnityEngine;

public class NPC : MonoBehaviour
{
	[SerializeField] private CharacterAnimator _anim = null;

	private Vector3 _moveFrom = Vector3.zero;
	private Vector3 _moveTo = Vector3.zero;
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

	public void MoveArc(Vector3 from, Vector3 to, float duration, bool faceRight, System.Action onFinished)
	{
		Show();
		_moveFrom = from;
		_moveTo = to;
		_anim.SetAlpha(1.0f);
		_anim.SetFlipX(!faceRight);
		_moveTimer = 0.0f;
		_moveDuration = duration;
		_onMovementFinished = onFinished;

		_arcCenter = 0.5f * (from + to);
		_arcCenter += Vector3.down;
		_moveFrom = from - _arcCenter;
		_moveTo = to - _arcCenter;
		_linear = false;
	}

	public void MoveLinear(Vector3 from, Vector3 to, float duration, float startAlpha, float endAlpha, bool faceRight, System.Action onFinished)
	{
		Show();
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
			transform.position = _arcCenter + Vector3.Slerp(_moveFrom, _moveTo, t);
		}

		if (_moveTimer >= _moveDuration)
		{
			_moveTimer = -1.0f;
			if (_onMovementFinished != null)
			{
				_onMovementFinished();
			}
		}
	}
}
