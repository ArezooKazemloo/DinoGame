using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shift temporarily speeds up the runner world without moving the player.</summary>
public class PlayerDash : MonoBehaviour
{
    [Min(1f)] public float boostMultiplier = 1.6f;

    public static float GameSpeedMultiplier { get; private set; } = 1f;

    [SerializeField] private Animator animator;
    private InputAction dashAction;
    private bool isBoosting;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        dashAction = new InputAction("Speed Boost", InputActionType.Button, "<Keyboard>/leftShift");
        dashAction.AddBinding("<Keyboard>/rightShift");
    }

    private void OnEnable() => dashAction?.Enable();

    private void Update()
    {
        bool shouldBoost = dashAction != null && dashAction.IsPressed() && IsPlaying();
        if (shouldBoost == isBoosting) return;

        isBoosting = shouldBoost;
        GameSpeedMultiplier = isBoosting ? boostMultiplier : 1f;
        if (isBoosting && animator != null) animator.SetTrigger("Dash");
    }

    private static bool IsPlaying() => GameManager.Instance == null || GameManager.Instance.State == GameManager.RunState.Playing;

    private void OnDisable()
    {
        dashAction?.Disable();
        isBoosting = false;
        GameSpeedMultiplier = 1f;
    }

    private void OnDestroy() => dashAction?.Dispose();
}
