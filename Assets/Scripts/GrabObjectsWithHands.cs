// GrabObjectsWithHands.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class GrabObjectsWithHands : MonoBehaviour
{
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XRHandJointID grabJoint = XRHandJointID.MiddleProximal;
    [SerializeField] private float grabColliderRadius = 0.08f;

    private XRHandSubsystem handSubsystem;
    private GameObject grabColliderObject;
    private SphereCollider grabCollider;

    private bool isNearObject = false;
    private bool isGrabbing = false;
    private Collider actualObject;

    [SerializeField] private HandPoseEvaluator handPoseEvaluator;
    [SerializeField] private float similarityValue;

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

        // Si está agarrando, fuerza la posición del objeto a la mano cada frame
        // independientemente de lo que pase con el trigger

        CalculateIsGrabing();

        
        if (isGrabbing && actualObject != null)
        {
            actualObject.transform.position = grabColliderObject.transform.position;
            actualObject.transform.rotation = grabColliderObject.transform.rotation;
        }
    }


    private void CalculateIsGrabing()
    {
        if(handPoseEvaluator.similarity >= similarityValue)
        {
            isGrabbing = true;
        }else
        {
            isGrabbing = false;
        }
    }

    // public void Grab()
    // {
    //     if (!isNearObject || actualObject == null) return;

    //     isGrabbing = true;
    //     actualObject.transform.SetParent(grabColliderObject.transform);
    //     actualObject.GetComponent<Rigidbody>().isKinematic = true;
    //     Debug.Log("Grabbed: " + actualObject.name);
    // }

    // // Solo suelta cuando el gesto de agarre termina explícitamente
    // public void Release()
    // {
    //     if (!isGrabbing || actualObject == null) return;

    //     isGrabbing = false;
    //     actualObject.transform.SetParent(null);
    //     actualObject.GetComponent<Rigidbody>().isKinematic = false;
    //     actualObject = null;
    //     Debug.Log("Released");
    // }

    public void OnObjectEnter(Collider other)
    {
        // Solo actualiza el objeto cercano si no estamos agarrando ya algo
        if (isGrabbing) return;
        isNearObject = true;
        actualObject = other;
        Debug.Log("Near: " + other.name);
    }

    public void OnObjectExit(Collider other)
    {
        // NUNCA sueltes el objeto si estás agarrando
        // El trigger exit se ignora completamente durante el agarre
        if (isGrabbing) return;
        if (actualObject == other)
        {
            isNearObject = false;
            actualObject = null;
        }
    }
}