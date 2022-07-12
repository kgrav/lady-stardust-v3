using UnityEngine;
using System;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

public class Flora : MonoBehaviour {

    public static Flora gameController => FindObjectOfType<Flora>();
    public static FloraController avatar => FindObjectOfType<FloraController>();
    public FloraController Character;
    public NVCam CharacterCamera;

    private const string MouseXInput = "Mouse X";
    private const string MouseYInput = "Mouse Y";
    private const string MouseScrollInput = "Mouse ScrollWheel";
    private const string HorizontalInput = "Horizontal";
    private const string VerticalInput = "Vertical";

    private void LateUpdate()
    {
        
    }
    private void Update()
        {

            HandleCharacterInput();
        }

    private void HandleCharacterInput()
    {
        FloraInputs characterInputs = new FloraInputs();

        // Build the CharacterInputs struct
        characterInputs.MoveAxisForward = Input.GetAxis(VerticalInput);
        characterInputs.MoveAxisRight = Input.GetAxis(HorizontalInput);
        characterInputs.JumpDown = Input.GetButtonDown("Jump");
        characterInputs.JumpUp = Input.GetButtonUp("Jump");
        characterInputs.SlideDown = Input.GetButtonDown("Slide");

        // Apply inputs to character
        Character.SetInputs(ref characterInputs);
    }
}
 
