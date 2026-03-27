using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleBoatUI : MonoBehaviour
{
    public FluidBoat targetBoat;

    [Header("UI References")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float joystickRange = 50f;

    [Header("Settings")]
    public float resetSpeed = 5.0f; // How fast joystick centers

    private Vector2 inputVector;
    private bool isTouching = false;
    private Vector2 joystickCenter;

    void Start()
    {
        if (targetBoat == null) targetBoat = FindObjectOfType<FluidBoat>();
        
        // Auto-create UI if missing (Quick Prototype Mode)
        if (joystickBackground == null)
        {
            CreateVirtualJoystick();
        }
        
        joystickCenter = joystickBackground.position;
    }

    void Update()
    {
        HandleInput();
        
        if (targetBoat != null)
        {
            // Map Y to Throttle (Forward/Back), X to Steer (Left/Right)
            targetBoat.throttleInput = inputVector.y;
            targetBoat.steerInput = inputVector.x;
        }
    }

    void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPos = touch.position;

            // Simple logic: If touch is roughly in the bottom-left/center area, treat as joystick
            // Or just check if we are touching the joystick rect
            if (touch.phase == TouchPhase.Began)
            {
                if (Vector2.Distance(touchPos, joystickCenter) < joystickRange * 2)
                {
                    isTouching = true;
                }
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (isTouching)
                {
                    Vector2 offset = touchPos - joystickCenter;
                    inputVector = Vector2.ClampMagnitude(offset, joystickRange) / joystickRange;
                    
                    // Update visual handle
                    joystickHandle.anchoredPosition = inputVector * joystickRange;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouching = false;
            }
        }
        else
        {
            // Mouse Fallback for Editor
            if (Input.GetMouseButtonDown(0))
            {
                if (Vector2.Distance(Input.mousePosition, joystickCenter) < joystickRange * 2)
                    isTouching = true;
            }
            else if (Input.GetMouseButton(0) && isTouching)
            {
                Vector2 offset = (Vector2)Input.mousePosition - joystickCenter;
                inputVector = Vector2.ClampMagnitude(offset, joystickRange) / joystickRange;
                joystickHandle.anchoredPosition = inputVector * joystickRange;
            }
            else
            {
                isTouching = false;
            }
        }

        if (!isTouching)
        {
            inputVector = Vector2.MoveTowards(inputVector, Vector2.zero, Time.deltaTime * resetSpeed);
            joystickHandle.anchoredPosition = inputVector * joystickRange;
        }
    }

    // Helper to create a quick UI if none exists
    void CreateVirtualJoystick()
    {
        GameObject canvasObj = new GameObject("BoatUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bgObj = new GameObject("Joystick_BG");
        bgObj.transform.SetParent(canvasObj.transform);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.3f);
        joystickBackground = bgObj.GetComponent<RectTransform>();
        joystickBackground.sizeDelta = new Vector2(150, 150);
        joystickBackground.anchorMin = new Vector2(0, 0);
        joystickBackground.anchorMax = new Vector2(0, 0);
        joystickBackground.pivot = new Vector2(0, 0);
        joystickBackground.anchoredPosition = new Vector2(100, 100);

        // Handle
        GameObject handleObj = new GameObject("Joystick_Handle");
        handleObj.transform.SetParent(bgObj.transform);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(1f, 1f, 1f, 0.8f);
        joystickHandle = handleObj.GetComponent<RectTransform>();
        joystickHandle.sizeDelta = new Vector2(70, 70);
        joystickHandle.anchoredPosition = Vector2.zero;
        
        // Update center ref
        // Note: Start() might run before layout update, so we use anchored pos roughly
        // Better to rely on transform.position in Update if possible, or force update
    }
}
