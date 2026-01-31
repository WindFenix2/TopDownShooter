using UnityEngine;

public class Sniper_SimpleBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main == null)
            return;

        Transform cam = Camera.main.transform;
        Vector3 fwd = cam.forward;
        fwd.y = 0;
        if (fwd.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }
}
