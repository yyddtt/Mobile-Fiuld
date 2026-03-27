using UnityEngine;

public class ObservationBoxMobileControl : MonoBehaviour
{
    public ObservationBox observationBox;
    public float horizontalFactor = 2.0f;
    public bool invertHorizontal = false;
    public float worldScale = 1f;
    public Camera cameraOverride;
    public bool autoInvertLandscape = true;
    public bool useWorldAxis = false;
    public bool requireSelectionToMove = true;
    public LayerMask selectableMask = ~0;

    Transform wallsRoot;
    Transform leftWall;
    Transform rightWall;
    float leftMinX;
    float leftMaxX;
    bool limitsInitialized;
    int activePointerId = -1;
    bool selectedLeftWall = false;

    void Start()
    {
        if (observationBox == null) observationBox = GetComponent<ObservationBox>();
        EnsureWallsRoot();
        EnsureLeftWall();
        EnsureRightWall();
        EnsureLimits();
    }

    void Update()
    {
        if (!Application.isMobilePlatform) return;
        EnsureWallsRoot();
        EnsureLeftWall();
        EnsureRightWall();
        EnsureLimits();
        if (leftWall == null || rightWall == null) return;
        var cam = cameraOverride != null ? cameraOverride : Camera.main;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (requireSelectionToMove)
            {
                if (activePointerId < 0 && t.phase == TouchPhase.Began)
                {
                    if (cam != null)
                    {
                        Ray r = cam.ScreenPointToRay(t.position);
                        RaycastHit hit;
                        if (Physics.Raycast(r, out hit, 1000f, selectableMask))
                        {
                            selectedLeftWall = (hit.transform == leftWall) || (leftWall != null && hit.transform.IsChildOf(leftWall));
                            if (selectedLeftWall) activePointerId = t.fingerId;
                        }
                    }
                }
                if (activePointerId >= 0 && t.fingerId == activePointerId)
                {
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        float nx = t.deltaPosition.x / Mathf.Max(Screen.width, 1);
                        float sx = invertHorizontal ? -1f : 1f;
                        Vector3 right = (cam != null && !useWorldAxis) ? cam.transform.right : Vector3.right;
                        var move = right * (nx * horizontalFactor * sx);
                        var scale = Mathf.Max(worldScale, 0.0001f);
                        var target = leftWall.position + move * scale;
                        var clampedX = Mathf.Clamp(target.x, leftMinX, leftMaxX);
                        leftWall.position = new Vector3(clampedX, leftWall.position.y, leftWall.position.z);
                        if (observationBox != null) observationBox.SyncBoundsFromWalls();
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        activePointerId = -1;
                        selectedLeftWall = false;
                    }
                }
            }
            else if (t.phase == TouchPhase.Moved)
            {
                float nx = t.deltaPosition.x / Mathf.Max(Screen.width, 1);
                float sx = invertHorizontal ? -1f : 1f;
                Vector3 right = (cam != null && !useWorldAxis) ? cam.transform.right : Vector3.right;
                var move = right * (nx * horizontalFactor * sx);
                var scale = Mathf.Max(worldScale, 0.0001f);
                var target = leftWall.position + move * scale;
                var clampedX = Mathf.Clamp(target.x, leftMinX, leftMaxX);
                leftWall.position = new Vector3(clampedX, leftWall.position.y, leftWall.position.z);
                if (observationBox != null) observationBox.SyncBoundsFromWalls();
            }
        }
    }

    void EnsureWallsRoot()
    {
        if (wallsRoot != null) return;
        if (observationBox == null) return;
        var t = observationBox.transform.Find("ObservationBoxWalls");
        wallsRoot = t != null ? t : null;
    }

    void EnsureLeftWall()
    {
        if (leftWall != null) return;
        if (wallsRoot == null) return;
        var t = wallsRoot.Find("WallLeft");
        leftWall = t != null ? t : null;
    }

    void EnsureRightWall()
    {
        if (rightWall != null) return;
        if (wallsRoot == null) return;
        var t = wallsRoot.Find("WallRight");
        rightWall = t != null ? t : null;
    }

    void EnsureLimits()
    {
        if (limitsInitialized) return;
        if (leftWall == null || rightWall == null || observationBox == null) return;
        leftMinX = leftWall.position.x;
        leftMaxX = rightWall.position.x - observationBox.thickness;
        limitsInitialized = true;
    }
}
