using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    struct JumpPadTarget
    {
        public float ContactTime;
        public Vector3 ContactVelocity;
    }

    [SerializeField] float LaunchDelay = 0.1f;
    [SerializeField] float LaunchForce = 100f;
    [SerializeField] float PlayerLaunchForceMultiplier = 5f;
    [SerializeField] ForceMode LaunchMode = ForceMode.Impulse;
    [SerializeField] float ImpactVelocityScale = 0.05f;
    [SerializeField] float MaxImpactVelocityScale = 2f;
    [SerializeField] float MaxDistortionWeight = 0.25f;

    Dictionary<Rigidbody, JumpPadTarget> Targets = new Dictionary<Rigidbody, JumpPadTarget>();

    List<Rigidbody> TargetsToClear = new List<Rigidbody>();
    private void FixedUpdate()
    {
        // check for targets to launch
        float thresholdTime = Time.timeSinceLevelLoad - LaunchDelay;
        foreach(var kvp in Targets)
        {
            if (kvp.Value.ContactTime >= thresholdTime)
            {
                Launch(kvp.Key, kvp.Value.ContactVelocity);
                TargetsToClear.Add(kvp.Key);
            }
        }

        // clear out launched targets
        foreach(var target in TargetsToClear)
            Targets.Remove(target);
        TargetsToClear.Clear();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // attempt to retrieve the rigid body
        Rigidbody rb;
        if (collision.gameObject.TryGetComponent<Rigidbody>(out rb))
        {
            Targets[rb] = new JumpPadTarget() { ContactTime = Time.timeSinceLevelLoad,
                                                ContactVelocity = collision.relativeVelocity };
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        
    }

    void Launch(Rigidbody targetRB, Vector3 contactVelocity)
    {
        Vector3 launchVector = transform.up;

        // calculate the distortion vector
        Vector3 distortionVector = transform.forward * Vector3.Dot(contactVelocity, transform.forward) +
                                   transform.right * Vector3.Dot(contactVelocity, transform.right);
        distortionVector = distortionVector.normalized * MaxDistortionWeight;
        launchVector = (launchVector + distortionVector).normalized;

        // project the relative velocity along the jump axis
        float contactProjection = Vector3.Dot(contactVelocity, transform.up);
        if (contactProjection < 0)
        {
            // scale up the launch vector based on how fast we hit
            launchVector *= Mathf.Min(MaxImpactVelocityScale, 1f + Mathf.Abs(contactProjection * ImpactVelocityScale));
        }

        if (targetRB.CompareTag("Player"))
            launchVector *= PlayerLaunchForceMultiplier;

        targetRB.AddForce(launchVector * LaunchForce, LaunchMode);
    }
}
