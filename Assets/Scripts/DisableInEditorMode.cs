using UnityEngine;

public class DisableInEditorMode : MonoBehaviour
{
    [SerializeField] private MonoBehaviour target;
    [SerializeField] private string reason;

    private void Awake()
    {
#if UNITY_EDITOR
        Debug.LogWarning(reason);
        target.enabled = false;
#endif
    }

    private void OnDestroy()
    {
        target.enabled = true;
    }
}
