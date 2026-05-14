// GrabTriggerDetector.cs
using UnityEngine;

public class GrabTriggerDetector : MonoBehaviour
{
    private GrabObjectsWithHands grabManager;

    public void Initialize(GrabObjectsWithHands manager)
    {
        grabManager = manager;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GrabObject") || other.CompareTag("JointObject"))
            grabManager.OnObjectEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GrabObject") || other.CompareTag("JointObject"))
            grabManager.OnObjectExit(other);
    }
}