using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class GameStart : MonoBehaviour
{
    [SerializeField] 
    private NoteMover noteMover;

    [SerializeField]
    private TextMeshProUGUI scoreText;

    private bool hasStarted = false;
    [SerializeField]
    private GameObject GameObject;
    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Update()
    {
        if (Touch.activeTouches.Count == 0) return;

        var touch = Touch.activeTouches[0];

        if (touch.phase == TouchPhase.Ended && !hasStarted)
        {
            GameObject.SetActive(false);
            scoreText.text = "Score:0";
            noteMover.GameStart();
            hasStarted = true;
        }
    }
}
