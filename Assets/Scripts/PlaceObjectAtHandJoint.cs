using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class PlaceObjectAtHandJoint : MonoBehaviour
{
    [SerializeField] private GameObject placementPrefab;
    [SerializeField] public Handedness handedness = Handedness.Right;
    [SerializeField] private XRHandJointID jointID = XRHandJointID.IndexTip;
    [SerializeField] private Transform placementParent;
    [SerializeField] private bool startEnabled = false;

    private XRHandSubsystem handSubsystem;
    private GameObject placementInstance;
    private bool m_IsActive = false;

    void OnEnable()
    {
        StartCoroutine(WaitForHandSubsystem());
        m_IsActive = startEnabled;
    }

    void OnDisable()
    {
        DisablePlacement();
    }

    IEnumerator WaitForHandSubsystem()
    {
        List<XRHandSubsystem> subsystems = new();

        while (handSubsystem == null)
        {
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var sub in subsystems)
            {
                if (sub != null && sub.running)
                {
                    handSubsystem = sub;
                    break;
                }
            }
            yield return null;
        }
    }

    public void EnablePlacement() => m_IsActive = true;

    public void DisablePlacement()
    {
        m_IsActive = false;
        if (placementInstance != null)
        {
            Destroy(placementInstance);
            placementInstance = null;
        }
    }

    void Update()
    {
        if (handSubsystem == null || !m_IsActive) return;

        XRHand hand = handedness == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked) return;

        XRHandJoint joint = hand.GetJoint(jointID);
        if (!joint.TryGetPose(out Pose pose)) return;

        if (placementInstance == null)
        {
            placementInstance = Instantiate(placementPrefab);
            if (placementParent != null)
                placementInstance.transform.SetParent(placementParent, true);
        }

        placementInstance.transform.SetPositionAndRotation(pose.position, pose.rotation);
    }
}