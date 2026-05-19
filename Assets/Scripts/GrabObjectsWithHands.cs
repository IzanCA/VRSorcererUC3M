using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;

public class GrabObjectsWithHands : MonoBehaviour
{
    [Header("Hand")]
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XRHandJointID grabJoint = XRHandJointID.MiddleProximal;
    [SerializeField] private float grabColliderRadius = 0.08f;

    [Header("Grab Objects")]
    [SerializeField] private float followSpeed = 35f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float maxVelocity = 12f;

    [Header("Joint Objects")]
    [SerializeField] private float jointForce = 800f;
    [SerializeField] private float jointDamping = 50f;

    [Header("Layers")]
    [SerializeField] private LayerMask ignoredLayers;

    [Header("References")]
    [SerializeField] private HandPoseEvaluator handPoseEvaluator;
    [SerializeField] private Slider sliderRadius;

    private XRHandSubsystem handSubsystem;
    private GameObject grabColliderObject;
    private SphereCollider grabCollider;

    private bool isNearObject;
    private bool isGrabbing;
    private bool isGrabbingJointed;

    private Collider actualObject;

    private Vector3 grabPositionOffset;
    private Quaternion grabRotationOffset;

    private void OnEnable()
    {
        StartCoroutine(WaitForHandSubsystem());
    }

    private IEnumerator WaitForHandSubsystem()
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

        CreateGrabCollider();
    }

    private void CreateGrabCollider()
    {
        string colliderName = $"GrabCollider_{handedness}";

        GameObject existing = GameObject.Find(colliderName);

        if (existing != null)
        {
            grabColliderObject = existing;
            grabCollider = existing.GetComponent<SphereCollider>();
            return;
        }

        grabColliderObject = new GameObject(colliderName);

        grabCollider = grabColliderObject.AddComponent<SphereCollider>();
        grabCollider.radius = grabColliderRadius;
        grabCollider.isTrigger = true;

        Rigidbody rb = grabColliderObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        GrabTriggerDetector detector =
            grabColliderObject.AddComponent<GrabTriggerDetector>();

        detector.Initialize(this);
    }

    private void Update()
    {
        if (handSubsystem == null || grabColliderObject == null)
            return;

        XRHand hand = handedness == Handedness.Left
            ? handSubsystem.leftHand
            : handSubsystem.rightHand;

        if (!hand.isTracked)
            return;

        XRHandJoint joint = hand.GetJoint(grabJoint);

        if (joint.TryGetPose(out Pose pose))
        {
            grabColliderObject.transform.SetPositionAndRotation(
                pose.position,
                pose.rotation
            );
        }

        if (sliderRadius != null)
            grabCollider.radius = sliderRadius.value / 10f;

        if (isGrabbing && actualObject == null)
            ResetGrab();

        CalculateIsGrabing();

        // JointObjects → lógica original
        if (isGrabbing && isGrabbingJointed && actualObject != null)
        {
            Rigidbody rb = actualObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 targetPosition =
                    grabColliderObject.transform.position +
                    grabColliderObject.transform.rotation *
                    grabPositionOffset;

                Vector3 positionError =
                    targetPosition -
                    actualObject.transform.position;

                Vector3 velocityError =
                    -rb.linearVelocity;

                Vector3 force =
                    (positionError * jointForce) +
                    (velocityError * jointDamping);

                rb.AddForce(force, ForceMode.Force);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isGrabbing || actualObject == null || isGrabbingJointed)
            return;

        Rigidbody rb = actualObject.GetComponent<Rigidbody>();

        if (rb == null)
            return;

        Vector3 targetPosition =
            grabColliderObject.transform.position +
            grabColliderObject.transform.rotation *
            grabPositionOffset;

        Quaternion targetRotation =
            grabColliderObject.transform.rotation *
            grabRotationOffset;

        Vector3 velocity =
            (targetPosition - rb.position) * followSpeed;

        rb.linearVelocity =
            Vector3.ClampMagnitude(
                velocity,
                maxVelocity
            );

        Quaternion deltaRotation =
            targetRotation *
            Quaternion.Inverse(rb.rotation);

        deltaRotation.ToAngleAxis(
            out float angle,
            out Vector3 axis
        );

        if (angle > 180f)
            angle -= 360f;

        if (axis != Vector3.zero)
        {
            rb.angularVelocity =
                axis *
                angle *
                Mathf.Deg2Rad *
                rotationSpeed;
        }
    }

    private void CalculateIsGrabing()
    {
        bool shouldGrab =
            isNearObject &&
            actualObject != null &&
            handPoseEvaluator.similarity >=
            handPoseEvaluator.thresholdSlider.value;

        if (!isGrabbing && shouldGrab)
        {
            isGrabbingJointed =
                actualObject.CompareTag("JointObject");

            isGrabbing = true;

            Quaternion inverseHand =
                Quaternion.Inverse(
                    grabColliderObject.transform.rotation
                );

            grabPositionOffset =
                inverseHand *
                (
                    actualObject.transform.position -
                    grabColliderObject.transform.position
                );

            grabRotationOffset =
                inverseHand *
                actualObject.transform.rotation;

            Rigidbody rb =
                actualObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.interpolation =
                    RigidbodyInterpolation.Interpolate;

                rb.collisionDetectionMode =
                    CollisionDetectionMode
                    .ContinuousDynamic;
            }
        }
        else if (isGrabbing && !shouldGrab)
        {
            Rigidbody rb =
                actualObject?.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            ResetGrab();
        }
    }

    private void ResetGrab()
    {
        isGrabbing = false;
        isNearObject = false;
        isGrabbingJointed = false;
        actualObject = null;
    }

    public void OnObjectEnter(Collider other)
    {
        if (isGrabbing)
            return;

        if (other.isTrigger)
            return;

        // Ignorar mano/bones/layers XR
        if ((ignoredLayers.value &
            (1 << other.gameObject.layer)) != 0)
        {
            return;
        }

        isNearObject = true;
        actualObject = other;
    }

    public void OnObjectExit(Collider other)
    {
        if (isGrabbing)
            return;

        if (actualObject == other)
        {
            isNearObject = false;
            actualObject = null;
        }
    }
}