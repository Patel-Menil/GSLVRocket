using UnityEngine;
using UnityEngine.EventSystems;

public class UIPartDragSource : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Prefab")]
    public GameObject partPrefab;

    private GameObject ghost;
    private CanvasGroup canvasGroup;
    private Plane dragPlane;
    private Camera cam;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        cam = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;

        Ray ray = cam.ScreenPointToRay(eventData.position);

        float spawnDepth = 3f;
        Vector3 mouseWorld = ray.GetPoint(spawnDepth);

        ghost = Instantiate(partPrefab);
        ghost.name = partPrefab.name;

        ghost.transform.rotation *= Quaternion.Euler(ghost.transform.rotation.x, ghost.transform.rotation.y, 180f);

        dragPlane = new Plane(-cam.transform.forward, mouseWorld);

        MoveBoundsCenterTo(mouseWorld);

        var pd = ghost.GetComponent<PartDrag>();
        if (pd != null)
            pd.locked = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost == null) return;

        Ray ray = cam.ScreenPointToRay(eventData.position);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 mouseWorld = ray.GetPoint(enter);
            MoveBoundsCenterTo(mouseWorld);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (ghost == null) return;

        var pd = ghost.GetComponent<PartDrag>();
        if (pd != null)
        {
            pd.locked = false;
            pd.TrySnapExternally();
        }

        ghost = null;
    }

    void MoveBoundsCenterTo(Vector3 target)
    {
        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 boundsCenter = bounds.center;
        Vector3 delta = target - boundsCenter;

        ghost.transform.position += delta;
    }
}
