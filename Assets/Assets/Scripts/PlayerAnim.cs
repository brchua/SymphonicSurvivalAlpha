using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationInput : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        int direction = 0; // idle

        if (kb.leftArrowKey.isPressed) direction = 1;
        else if (kb.rightArrowKey.isPressed) direction = 2;
        else if (kb.upArrowKey.isPressed) direction = 3;
        else if (kb.downArrowKey.isPressed) direction = 4;

        animator.SetInteger("Direction", direction);
    }
}