using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class AnimalAmbientAnimation : MonoBehaviour
{
    private static readonly int AnimationParameter = Animator.StringToHash("animation");
    private const int Idle = 0;
    private const int Eat = 4;
    private const int Rest = 5;

    [SerializeField] private Vector2 intervalRange = new Vector2(4f, 9f);
    [SerializeField] private bool playImmediately = true;

    private Animator animator;
    private float nextSwitchTime;
    private int currentAnimation;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        currentAnimation = Idle;

        if (playImmediately && animator != null)
        {
            animator.SetInteger(AnimationParameter, currentAnimation);
        }

        ScheduleNextSwitch();
    }

    private void Update()
    {
        if (animator == null || Time.time < nextSwitchTime)
        {
            return;
        }

        currentAnimation = PickNextAnimation();
        animator.SetInteger(AnimationParameter, currentAnimation);
        ScheduleNextSwitch();
    }

    private int PickNextAnimation()
    {
        if (currentAnimation == Eat || currentAnimation == Rest)
        {
            return Idle;
        }

        return Random.Range(0, 2) == 0 ? Eat : Rest;
    }

    private void ScheduleNextSwitch()
    {
        nextSwitchTime = Time.time + Random.Range(intervalRange.x, intervalRange.y);
    }
}