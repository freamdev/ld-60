
using UnityEngine;

public class WorldCanvasBillboard : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool lockYRotation = false;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (playerCamera == null)
            return;

        if (!lockYRotation)
        {
            transform.rotation = playerCamera.transform.rotation;
        }
        else
        {
            var direction = transform.position - playerCamera.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
