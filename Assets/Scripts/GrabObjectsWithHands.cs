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

    [Header("Joint Object Physics")]
    [SerializeField] private float jointForce = 800f;
    [SerializeField] private float jointDamping = 50f;

    [Header("Auto Release")]
    [SerializeField] private float maxGrabDistance = 0.25f; // distancia máxima antes de soltar

    private XRHandSubsystem handSubsystem;
    private GameObject grabColliderObject;
    private SphereCollider grabCollider;

    private bool isNearObject = false;
    private bool isGrabbing = false;
    private bool isGrabbingJointed = false;

    private Collider actualObject;

    private Vector3 grabPositionOffset;
    private Quaternion grabRotationOffset;

    private Vector3 jointLocalGrabPoint;
    private Quaternion jointRotationOffset;

    [Header("Hand Pose")]
    [SerializeField] private HandPoseEvaluator handPoseEvaluator;

    [Header("Debug")]
    [SerializeField] private Slider sliderRadius;

    private float similarityValue;

    private void OnEnable() => StartCoroutine(WaitForHandSubsystem());

    private void OnDisable()
    {
        if (grabColliderObject != null)
            Destroy(grabColliderObject);
    }

    private IEnumerator WaitForHandSubsystem()
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

    private void CreateGrabCollider()
    {
        grabColliderObject = new GameObject("GrabCollider_" + handedness);
        grabCollider = grabColliderObject.AddComponent<SphereCollider>();
        grabCollider.radius = grabColliderRadius;
        grabCollider.isTrigger = true;

        Rigidbody rb = grabColliderObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GrabTriggerDetector detector = grabColliderObject.AddComponent<GrabTriggerDetector>();
        detector.Initialize(this);

        string handName = handedness == Handedness.Left ? "LeftHand" : "RightHand";
        GameObject handRoot = GameObject.Find(handName);
        if (handRoot != null)
            grabColliderObject.transform.position = handRoot.transform.position;
    }

    private void Update()
    {
        if (handSubsystem == null || grabColliderObject == null) return;

        XRHand hand = handedness == Handedness.Left ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked) return;

        XRHandJoint joint = hand.GetJoint(grabJoint);
        if (joint.TryGetPose(out Pose pose))
            grabColliderObject.transform.SetPositionAndRotation(pose.position, pose.rotation);

        if (sliderRadius != null)
            grabCollider.radius = sliderRadius.value / 10f;

        if (isGrabbing && actualObject == null)
        {
            isGrabbing = false;
            isNearObject = false;
            isGrabbingJointed = false;
        }

        // Si el objeto se aleja demasiado de la mano, suéltalo
        if (isGrabbing && actualObject != null)
        {
            float distance = Vector3.Distance(
                grabColliderObject.transform.position,
                actualObject.transform.position
            );

            if (distance > maxGrabDistance)
                ForceRelease();
        }

        CalculateIsGrabing();
    }

    private void FixedUpdate()
    {
        if (!isGrabbing || actualObject == null) return;

        Rigidbody rb = actualObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (!isGrabbingJointed)
        {
            Vector3 targetPosition = grabColliderObject.transform.position +
                grabColliderObject.transform.rotation * grabPositionOffset;
            Quaternion targetRotation = grabColliderObject.transform.rotation * grabRotationOffset;

            rb.linearVelocity = (targetPosition - rb.position) / Time.fixedDeltaTime;

            Quaternion rotationDiff = targetRotation * Quaternion.Inverse(rb.rotation);
            rotationDiff.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (axis != Vector3.zero && !float.IsNaN(axis.x))
                rb.angularVelocity = axis * (angle * Mathf.Deg2Rad) / Time.fixedDeltaTime;
        }
        else
        {
            Vector3 worldGrabPoint = actualObject.transform.TransformPoint(jointLocalGrabPoint);
            Vector3 positionError = grabColliderObject.transform.position - worldGrabPoint;
            Vector3 velocityError = -rb.linearVelocity;
            Vector3 force = (positionError * jointForce) + (velocityError * jointDamping);
            rb.AddForceAtPosition(force, worldGrabPoint, ForceMode.Force);

            Quaternion targetRotation = grabColliderObject.transform.rotation * jointRotationOffset;
            Quaternion rotationError = targetRotation * Quaternion.Inverse(actualObject.transform.rotation);
            rotationError.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (axis != Vector3.zero && !float.IsNaN(axis.x))
            {
                Vector3 torque = axis * angle * Mathf.Deg2Rad * jointForce;
                rb.AddTorque(torque - rb.angularVelocity * jointDamping, ForceMode.Force);
            }
        }
    }

    private void ForceRelease()
    {
        if (actualObject == null) return;

        isGrabbing = false;
        isNearObject = false;

        if (!isGrabbingJointed)
        {
            Rigidbody rb = actualObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        isGrabbingJointed = false;
        actualObject = null;
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

            Quaternion inverseHandRotation = Quaternion.Inverse(grabColliderObject.transform.rotation);
            grabPositionOffset = inverseHandRotation * (actualObject.transform.position - grabColliderObject.transform.position);
            grabRotationOffset = inverseHandRotation * actualObject.transform.rotation;

            jointLocalGrabPoint = actualObject.transform.InverseTransformPoint(grabColliderObject.transform.position);
            jointRotationOffset = Quaternion.Inverse(grabColliderObject.transform.rotation) * actualObject.transform.rotation;

            if (!isGrabbingJointed)
            {
                Rigidbody rb = actualObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.isKinematic = false;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
            }
        }
        else if (wasGrabbing && !shouldGrab && actualObject != null)
        {
            isGrabbing = false;
            isNearObject = false;

            if (!isGrabbingJointed)
            {
                Rigidbody rb = actualObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
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