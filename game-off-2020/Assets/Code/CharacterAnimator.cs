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

	[SerializeField] private SpriteRenderer _sprite = null;
	[SerializeField] private Animator _animator = null;
	[SerializeField] private RuntimeAnimatorController[] _animControllers = null;

	private CharacterSelection _character = CharacterSelection.Beige;

	private static readonly int ANIM_FALL = Animator.StringToHash("Fall");
	private static readonly int ANIM_IDLE = Animator.StringToHash("Idle");
	private static readonly int ANIM_JUMP = Animator.StringToHash("Jump");
	private static readonly int ANIM_WALK = Animator.StringToHash("Walk");

	public void SetCharacter(CharacterSelection character)
	{
		_character = character;
		_animator.runtimeAnimatorController = _animControllers[(int)_character];
		PlayIdle();
	}

	public void SetFlipX(bool flip)
	{
		_sprite.flipX = flip;
	}

	public void SetAlpha(float alpha)
	{
		_sprite.color = new Color(_sprite.color.r, _sprite.color.g, _sprite.color.b, alpha);
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

	public static CharacterSelection GetRandomNPC()
	{
		CharacterSelection[] npcs = {
			CharacterSelection.Beige,
			CharacterSelection.Blue,
			//CharacterSelection.Green,
			CharacterSelection.Pink,
			CharacterSelection.Yellow
		};
		return npcs[Random.Range(0, npcs.Length)];
	}
}
