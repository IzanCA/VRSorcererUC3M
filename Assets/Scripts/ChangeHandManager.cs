// ChangeHandManager.cs
using TMPro;
using UnityEngine;
using UnityEngine.XR.Hands;

public class ChangeHandManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private XRHandTrackingEvents xRHandTrackingEventsRight;
    [SerializeField] private XRHandTrackingEvents xRHandTrackingEventsLeft;
    [SerializeField] private GameObject placeObjectAtHandJointRight;
    [SerializeField] private GameObject placeObjectAtHandJointLeft;
    [SerializeField] private GameObject StaticHandsDetectorsRight;
    [SerializeField] private GameObject StaticHandsDetectorsLeft;
    [SerializeField] private GameObject HandsEvaluatorsLeft;
    [SerializeField] private GameObject HandsEvaluatorsRight;

    // 0 = Right, 1 = Left, 2 = Both
    public int CurrentHandMode => dropdown.value;

    void Update()
    {
        switch (dropdown.value)
        {
            case 0: SetHands(right: true,  left: false); break;
            case 1: SetHands(right: false, left: true);  break;
            case 2: SetHands(right: true,  left: true);  break;
        }
    }

    private void SetHands(bool right, bool left)
    {
        xRHandTrackingEventsRight.gameObject.SetActive(right);
        xRHandTrackingEventsLeft.gameObject.SetActive(left);

        if(placeObjectAtHandJointLeft != null && placeObjectAtHandJointRight != null)
        {
            placeObjectAtHandJointRight.SetActive(right);
            placeObjectAtHandJointLeft.SetActive(left);
        }

        StaticHandsDetectorsRight.SetActive(right);
        StaticHandsDetectorsLeft.SetActive(left);
        HandsEvaluatorsRight.SetActive(right);
        HandsEvaluatorsLeft.SetActive(left);
    }
}