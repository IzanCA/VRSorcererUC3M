// GrabObjectsWithHands.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;

public class GrabObjectsWithHands : MonoBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XRHandJointID grabJoint = XRHandJointID.MiddleProximal;
    [SerializeField] private float grabColliderRadius = 0.08f;
    [SerializeField] private float jointForce = 800f;
    [SerializeField] private float jointDamping = 50f;

    private XRHandSubsystem handSubsystem;
    private GameObject grabColliderObject;
    private SphereCollider grabCollider;

    private bool isNearObject = false;
    private bool isGrabbing = false;
    private bool isGrabbingJointed = false;
    private Collider actualObject;

    [SerializeField] private HandPoseEvaluator handPoseEvaluator;
    [SerializeField] private Slider sliderRadius;
    private float similarityValue;

    void OnEnable() => StartCoroutine(WaitForHandSubsystem());

    void OnDisable()
    {
        if (grabColliderObject != null)
            Destroy(grabColliderObject);
    }

    IEnumerator WaitForHandSubsystem()
    {
        List<XRHandSubsystem> subsystems = new();
        while (handSubsystem == null)
        {
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var sub in subsystems)
                if (sub != null && sub.running) { handSubsystem = sub; break; }
            yield return null;
        }
        CreateGrabCollider();
    }

    void CreateGrabCollider()
    {
        grabColliderObject = new GameObject("GrabCollider_" + handedness);
        grabCollider = grabColliderObject.AddComponent<SphereCollider>();
        grabCollider.radius = grabColliderRadius;
        grabCollider.isTrigger = true;

        var rb = grabColliderObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var detector = grabColliderObject.AddComponent<GrabTriggerDetector>();
        detector.Initialize(this);

        string handName = handedness == Handedness.Left ? "LeftHand" : "RightHand";
        GameObject handRoot = GameObject.Find(handName);
        if (handRoot != null)
            grabColliderObject.transform.position = handRoot.transform.position;
    }

    void Update()
    {
        if (handSubsystem == null || grabColliderObject == null) return;

        XRHand hand = handedness == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked) return;

        XRHandJoint joint = hand.GetJoint(grabJoint);
        if (joint.TryGetPose(out Pose pose))
            grabColliderObject.transform.SetPositionAndRotation(pose.position, pose.rotation);

        if (sliderRadius != null)
            grabCollider.radius = sliderRadius.value / 10f;

        // Si el objeto fue destruido mientras lo agarrábamos, resetea todo
        if (isGrabbing && actualObject == null)
        {
            isGrabbing = false;
            isNearObject = false;
            isGrabbingJointed = false;
        }

        CalculateIsGrabing();

        // Objeto normal — lo movemos con la mano
        if (isGrabbing && !isGrabbingJointed && actualObject != null)
        {
            actualObject.transform.position = grabColliderObject.transform.position;
            actualObject.transform.rotation = grabColliderObject.transform.rotation;
        }

        // Puerta/cajón — PD controller con amortiguación
        if (isGrabbing && isGrabbingJointed && actualObject != null)
        {
            var rb = actualObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 positionError = grabColliderObject.transform.position - actualObject.transform.position;
                Vector3 velocityError = -rb.linearVelocity;

                Vector3 force = (positionError * jointForce) + (velocityError * jointDamping);
                rb.AddForce(force, ForceMode.Force);
            }
        }
    }

    private void CalculateIsGrabing()
    {
        similarityValue = handPoseEvaluator.thresholdSlider.value;

        bool wasGrabbing = isGrabbing;
        bool shouldGrab = isNearObject && actualObject != null && handPoseEvaluator.similarity >= similarityValue;

        if (!wasGrabbing && shouldGrab && actualObject != null)
        {
            isGrabbingJointed = actualObject.CompareTag("JointObject");
            isGrabbing = true;

            if (!isGrabbingJointed)
            {
                actualObject.transform.SetParent(grabColliderObject.transform);
                actualObject.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
        else if (wasGrabbing && !shouldGrab && actualObject != null)
        {
            isGrabbing = false;
            isNearObject = false;

            if (!isGrabbingJointed)
            {
                var rb = actualObject.GetComponent<Rigidbody>();
                actualObject.transform.SetParent(null);
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            isGrabbingJointed = false;
            actualObject = null;
        }
    }

    public void OnObjectEnter(Collider other)
    {
        if (isGrabbing) return;
        isNearObject = true;
        actualObject = other;
    }

    public void OnObjectExit(Collider other)
    {
        if (isGrabbing) return;
        if (actualObject == other)
        {
            isNearObject = false;
            actualObject = null;
        }
    }
}