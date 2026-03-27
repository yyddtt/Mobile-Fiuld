using UnityEngine;

public class FluidStirrer : MonoBehaviour
{
    public MPMFluidMobile fluid;
    
    [Header("Interaction Settings")]
    [Tooltip("Radius of the interaction sphere.")]
    public float radius = 0.8f;
    
    [Tooltip("Height of the water surface for interaction.")]
    public float interactionDepth = 2.0f; 
    
    [Range(0f, 5f)] 
    public float velocityMultiplier = 1.0f;

    private Plane waterPlane;
    private Vector3 lastPos;
    private bool isDragging = false;

    void Start()
    {
        if (fluid == null) fluid = FindObjectOfType<MPMFluidMobile>();
        // Initialize water plane at interaction depth
        // Normal is Up, passing through point (0, interactionDepth, 0)
        waterPlane = new Plane(Vector3.up, new Vector3(0, interactionDepth, 0));
    }

    void Update()
    {
        if (fluid == null) return;

        // Mouse Down: Start Dragging
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float enter;
            if (waterPlane.Raycast(ray, out enter))
            {
                isDragging = true;
                lastPos = ray.GetPoint(enter);
                UpdateStirrer(lastPos, Vector3.zero);
            }
        }
        // Mouse Drag: Update Position & Velocity
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float enter;
            if (waterPlane.Raycast(ray, out enter))
            {
                Vector3 currentPos = ray.GetPoint(enter);
                
                // Calculate velocity based on movement
                // Use a small epsilon to prevent division by zero
                float dt = Mathf.Max(Time.deltaTime, 1e-5f);
                Vector3 velocity = (currentPos - lastPos) / dt;
                
                UpdateStirrer(currentPos, velocity);
                lastPos = currentPos;
            }
        }
        // Mouse Up: Reset
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            fluid.stirrerSphere = Vector4.zero;
            fluid.stirrerVelocity = Vector3.zero;
        }
    }

    void UpdateStirrer(Vector3 pos, Vector3 vel)
    {
        fluid.stirrerSphere = new Vector4(pos.x, pos.y, pos.z, radius);
        fluid.stirrerVelocity = vel * velocityMultiplier;
    }

    void OnDrawGizmos()
    {
        if (fluid != null && fluid.stirrerSphere.w > 0)
        {
            Gizmos.color = Color.cyan;
            Vector4 s = fluid.stirrerSphere;
            Gizmos.DrawWireSphere(new Vector3(s.x, s.y, s.z), s.w);
            Gizmos.DrawLine(new Vector3(s.x, s.y, s.z), new Vector3(s.x, s.y, s.z) + fluid.stirrerVelocity * 0.1f);
        }
    }
}
