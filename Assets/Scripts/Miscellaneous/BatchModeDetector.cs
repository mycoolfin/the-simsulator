using UnityEngine;

public class BatchModeDetector : MonoBehaviour
{
    private enum Behaviour { DisableIfBatchMode, DisableIfNotBatchMode }

    [SerializeField] private Behaviour behaviour = Behaviour.DisableIfBatchMode;

    private void Awake()
    {
        if (Application.isBatchMode && behaviour == Behaviour.DisableIfBatchMode
        || !Application.isBatchMode && behaviour == Behaviour.DisableIfNotBatchMode)
        {
            gameObject.SetActive(false);
            return;
        }
    }
}
