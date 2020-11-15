using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
	public enum CharacterSelection
	{
		Beige,
		Blue,
		Green,
		Pink,
		Yellow
	}

	[SerializeField] private CharacterSelection _character = CharacterSelection.Green;
	[SerializeField] private Animator _animator = null;
	[SerializeField] private RuntimeAnimatorController[] _animControllers = null;

	private static readonly int ANIM_FALL = Animator.StringToHash("Fall");
	private static readonly int ANIM_IDLE = Animator.StringToHash("Idle");
	private static readonly int ANIM_JUMP = Animator.StringToHash("Jump");
	private static readonly int ANIM_WALK = Animator.StringToHash("Walk");

	private void Awake()
	{
		_animator.runtimeAnimatorController = _animControllers[(int)_character];
		PlayIdle();
	}

	public void PlayFall()
	{
		Play(ANIM_FALL);
	}

	public void PlayIdle()
	{
		Play(ANIM_IDLE);
	}

	public void PlayJump()
	{
		Play(ANIM_JUMP);
	}

	public void PlayWalk()
	{
		Play(ANIM_WALK);
	}

	private void Play(int anim)
	{
		if (_animator != null && _animator.isActiveAndEnabled)
		{
			_animator.Play(anim);
		}
	}
}
