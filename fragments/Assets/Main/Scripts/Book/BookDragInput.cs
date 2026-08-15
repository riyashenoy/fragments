using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace Fragments.Book
{
    /// <summary>
    /// Handles mouse drag and XR hand input to control page turning with realistic peeling.
    /// Casts rays to find the page, converts hits to material coordinates, and drives deformation.
    /// </summary>
    public class BookDragInput : MonoBehaviour
    {
        [SerializeField] private Book book;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask pageLayer = -1; // Set this to catch page meshes

        private BookSheet activeDrag;
        private Vector2 dragMaterialPoint;

        private void OnEnable()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Begin drag: raycast to find what we're hitting
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryBeginDrag(mousePos);
            }

            // Continue drag: update the target
            if (activeDrag != null && Mouse.current.leftButton.isPressed)
            {
                UpdateDrag(mousePos);
            }

            // End drag: release
            if (Mouse.current.leftButton.wasReleasedThisFrame && activeDrag != null)
            {
                EndDrag();
            }
        }

        private void TryBeginDrag(Vector2 screenPos)
        {
            if (book.Busy || book.NextSheet == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            // Raycast to the next sheet (the one to be turned)
            BookSheet target = book.NextSheet;
            if (target == null) return;

            // Check if we hit this sheet's mesh
            MeshCollider mc = target.GetComponent<MeshCollider>();
            if (mc == null)
            {
                // No collider — add temp one for raycasting
                mc = target.gameObject.AddComponent<MeshCollider>();
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, pageLayer == -1 ? LayerMask.GetMask("Default") : pageLayer))
            {
                if (hit.collider.GetComponent<BookSheet>() == target)
                {
                    // Convert world hit to material (2D page) coordinates
                    Vector3 localHit = target.transform.parent.InverseTransformPoint(hit.point);
                    dragMaterialPoint = new Vector2(localHit.x, localHit.z);

                    // Clamp to valid page bounds
                    dragMaterialPoint.x = Mathf.Clamp(dragMaterialPoint.x, -target.width * 0.5f, target.width * 0.5f);
                    dragMaterialPoint.y = Mathf.Clamp(dragMaterialPoint.y, -target.height * 0.5f, target.height * 0.5f);

                    activeDrag = target;
                    activeDrag.BeginDrag(dragMaterialPoint, hit.point);
                }
            }
        }

        private void UpdateDrag(Vector2 screenPos)
        {
            if (activeDrag == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            // Cast plane at the sheet's position
            Plane dragPlane = new Plane(
                mainCamera.transform.forward,
                activeDrag.transform.parent.position
            );

            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.origin + ray.direction * distance;
                activeDrag.DragTo(worldPoint);
            }
        }

        private void EndDrag()
        {
            if (activeDrag == null) return;

            // Release: sheet will animate to rest or completion
            activeDrag = null;
        }

        /// <summary>
        /// XR API: Hand tracking calls these directly.
        /// BeginPeel: hand pinch down on the page
        /// UpdatePeel: move hand while pinching
        /// EndPeel: release pinch
        /// </summary>
        public void BeginPeel(Vector3 worldPosition)
        {
            if (book.Busy || book.NextSheet == null) return;

            BookSheet target = book.NextSheet;

            // Convert world position to material coordinates
            Vector3 localHit = target.transform.parent.InverseTransformPoint(worldPosition);
            dragMaterialPoint = new Vector2(localHit.x, localHit.z);
            dragMaterialPoint.x = Mathf.Clamp(dragMaterialPoint.x, -target.width * 0.5f, target.width * 0.5f);
            dragMaterialPoint.y = Mathf.Clamp(dragMaterialPoint.y, -target.height * 0.5f, target.height * 0.5f);

            activeDrag = target;
            activeDrag.BeginDrag(dragMaterialPoint, worldPosition);
        }

        public void UpdatePeel(Vector3 worldPosition)
        {
            if (activeDrag != null)
                activeDrag.DragTo(worldPosition);
        }

        public void EndPeel()
        {
            activeDrag = null;
        }
    }
}