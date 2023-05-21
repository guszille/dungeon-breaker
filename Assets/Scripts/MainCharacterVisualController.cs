using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCharacterVisualController : MonoBehaviour
{
    private const string IS_RUNNING_PARAM = "IsRunning";

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        MainCharacterController.Instance.OnStateChanged += MainCharacter_OnStateChanged;
    }

    private void MainCharacter_OnStateChanged(object sender, System.EventArgs args)
    {
        bool isRunning = MainCharacterController.Instance.GetState() == MainCharacterController.State.RUNNING;
        animator.SetBool(IS_RUNNING_PARAM, isRunning);
    }
}
