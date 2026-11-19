using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A hinged door for VR. Put this on the door leaf, with its pivot on the hinge line.
/// Hold grip and move your hand to swing the door; tap grip to open or close it automatically.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRDoor : MonoBehaviour
{
    [Tooltip("Fully open angle in degrees around local Y. The sign sets the swing direction.")]
    [SerializeField] float openAngle = 95f;

    [Tooltip("Speed of the automatic swing after a tap, in degrees per second.")]
    [SerializeField] float autoSwingSpeed = 180f;

    [Tooltip("A grab shorter than this many seconds, with little movement, counts as a tap.")]
    [SerializeField] float tapMaxDuration = 0.35f;

    [Tooltip("Released within this many degrees of closed, the door snaps shut.")]
    [SerializeField] float closeSnapAngle = 4f;

    const float TapMaxSwing = 3f;
    const float MinHandDistanceFromHinge = 0.05f;

    XRSimpleInteractable interactable;
    Quaternion closedLocalRotation;
    float currentAngle;
    float targetAngle;

    Transform grabbingHand;
    Vector3 lastHandDirection;
    float grabStartTime;
    float grabStartAngle;

    public bool IsOpen => Mathf.Abs(targetAngle) > closeSnapAngle;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        closedLocalRotation = transform.localRotation;
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrabbed);
        interactable.selectExited.RemoveListener(OnReleased);
        grabbingHand = null;
    }

    public void Open() => targetAngle = openAngle;

    public void Close() => targetAngle = 0f;

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        grabbingHand = args.interactorObject.GetAttachTransform(interactable);
        grabStartTime = Time.time;
        grabStartAngle = currentAngle;
        targetAngle = currentAngle;
        TryGetHandDirection(out lastHandDirection);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Another hand may have taken over the door in the same frame.
        if (interactable.isSelected) return;

        grabbingHand = null;

        bool wasTap = Time.time - grabStartTime <= tapMaxDuration
                      && Mathf.Abs(currentAngle - grabStartAngle) <= TapMaxSwing;

        if (wasTap) Toggle();
        else targetAngle = Mathf.Abs(currentAngle) <= closeSnapAngle ? 0f : currentAngle;
    }

    void Update()
    {
        if (grabbingHand != null) FollowHand();
        else currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, autoSwingSpeed * Time.deltaTime);

        transform.localRotation = closedLocalRotation * Quaternion.Euler(0f, currentAngle, 0f);
    }

    void FollowHand()
    {
        if (!TryGetHandDirection(out var handDirection)) return;

        float delta = Vector3.SignedAngle(lastHandDirection, handDirection, transform.up);
        lastHandDirection = handDirection;

        currentAngle = Mathf.Clamp(currentAngle + delta, Mathf.Min(0f, openAngle), Mathf.Max(0f, openAngle));
        targetAngle = currentAngle;
    }

    bool TryGetHandDirection(out Vector3 direction)
    {
        direction = Vector3.ProjectOnPlane(grabbingHand.position - transform.position, transform.up);
        if (direction.magnitude < MinHandDistanceFromHinge) return false;
        direction.Normalize();
        return true;
    }
}
