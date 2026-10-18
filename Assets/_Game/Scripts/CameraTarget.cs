
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothSpeed = 10f;

    private void LateUpdate()
    {
        if (player == null)
            return;

        var t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            player.position,
            t
        );

        transform.rotation = Quaternion.Euler(
            0f,
            player.eulerAngles.y,
            0f
        );
    }
}
