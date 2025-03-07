using UnityEngine;

public class FakeLight : MonoBehaviour
{
    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.down);
    }
}
