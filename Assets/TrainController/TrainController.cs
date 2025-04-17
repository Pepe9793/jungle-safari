using UnityEngine;
using UnityEngine.Splines;

public class TrainController : MonoBehaviour
{
    public Transform[] wheels;
    public float wheelRotationSpeedMultiplier = 360f;

    [Header("Passenger Coaches")]
    public Transform[] coaches; // Passenger coaches
    public float coachSpacing = 2f; // Distance between each coach


    [Header("Coach Axis Configuration")]
    public AlignAxis CoachForwardAxis = AlignAxis.ZAxis;
    public AlignAxis CoachUpAxis = AlignAxis.YAxis;


    public SplineContainer Container;
    public float Speed = 5f;
    public LoopMode Loop = LoopMode.Loop;
    public EasingMode Easing = EasingMode.None;
    public AlignmentMode Alignment = AlignmentMode.SplineElement;
    public bool PlayOnAwake = true;

    [Header("Axis Configuration")]
    public AlignAxis ObjectForwardAxis = AlignAxis.ZAxis;
    public AlignAxis ObjectUpAxis = AlignAxis.YAxis;

    [Header("Timing")]
    [Range(0, 1)] public float StartOffset;

    [Header("Deceleration Settings")]
    public bool UseDecelerationTrigger = false;
    public float MinSpeed = 1f;

    [Header("Station Stop Settings")]
    public bool StopAtStation = false;
    public float WaitTimeAtStation = 3f;
    public bool ReverseAfterStation = false;


    // Events
    public event System.Action Completed;

    SplinePath<Spline> m_SplinePath;
    float m_SplineLength;
    float m_CurrentDistance;
    float m_TotalLength;
    bool m_Playing;
    Quaternion m_InitialRotation;

    bool isDecelerating = false;
    float originalSpeed;

    void Awake()
    {
        m_InitialRotation = transform.rotation;
        if (PlayOnAwake) Play();
    }

    void OnEnable()
    {
        originalSpeed = Speed;
        RebuildSplinePath();

        if (m_SplineLength > 0)
        {
            m_TotalLength = m_SplineLength;
            m_CurrentDistance = StartOffset * m_TotalLength;
        }
    }

    void Update()
    {
        if (!m_Playing || m_SplinePath == null) return;

        float deltaDistance = Speed * Time.deltaTime;
        m_CurrentDistance += deltaDistance;

        if (Loop == LoopMode.Once && m_CurrentDistance >= m_TotalLength)
        {
            m_CurrentDistance = m_TotalLength;
            m_Playing = false;
            Completed?.Invoke();
        }
        else if (Loop == LoopMode.Loop)
        {
            m_CurrentDistance %= m_TotalLength;

            // Reset state to allow next deceleration
            isDecelerating = false;
            Speed = originalSpeed;
        }

        else if (Loop == LoopMode.PingPong)
        {
            // Optional: PingPong support
        }

        UpdateCoaches();

        UpdateTransformByDistance();
        UpdateWheels();
    }

    void UpdateCoaches()
    {
        if (coaches == null || coaches.Length == 0 || m_SplinePath == null) return;

        for (int i = 0; i < coaches.Length; i++)
        {
            float coachDistance = m_CurrentDistance - coachSpacing * (i + 1);
            if (Loop == LoopMode.Loop)
                coachDistance = (coachDistance + m_TotalLength) % m_TotalLength;
            else
                coachDistance = Mathf.Clamp(coachDistance, 0f, m_TotalLength);

            float t = Mathf.Clamp01(coachDistance / m_TotalLength);
            float easedT = ApplyEasing(t);

            Vector3 position = Container.EvaluatePosition(m_SplinePath, easedT);
            Vector3 forward = Vector3.forward;
            Vector3 up = Vector3.up;

            switch (Alignment)
            {
                case AlignmentMode.SplineElement:
                    forward = Container.EvaluateTangent(m_SplinePath, easedT);
                    up = Container.EvaluateUpVector(m_SplinePath, easedT);
                    break;
                case AlignmentMode.SplineObject:
                    forward = Container.transform.forward;
                    up = Container.transform.up;
                    break;
                case AlignmentMode.World:
                    forward = Vector3.forward;
                    up = Vector3.up;
                    break;
            }

            Quaternion axisRemap = Quaternion.Inverse(Quaternion.LookRotation(
                GetAxisVector(CoachForwardAxis),
                GetAxisVector(CoachUpAxis)
            ));

            Quaternion rotation = Quaternion.LookRotation(forward, up) * axisRemap;

            coaches[i].position = position;
            coaches[i].rotation = rotation;
        }
    }


    void UpdateWheels()
    {
        foreach (var wheel in wheels)
        {
            float rotation = Speed * wheelRotationSpeedMultiplier * Time.deltaTime;
            wheel.Rotate(Vector3.down, rotation); // Adjust axis as needed
        }
    }
    void RebuildSplinePath()
    {
        if (Container != null && Container.Splines.Count > 0)
        {
            m_SplinePath = new SplinePath<Spline>(Container.Splines);
            m_SplineLength = m_SplinePath.GetLength();
        }
    }

    void UpdateTransformByDistance()
    {
        float t = Mathf.Clamp01(m_CurrentDistance / m_TotalLength);
        float easedT = ApplyEasing(t);

        var position = Container.EvaluatePosition(m_SplinePath, easedT);
        var rotation = GetTargetRotation(easedT);

        transform.position = position;
        transform.rotation = rotation;
    }

    float ApplyEasing(float t)
    {
        return Easing switch
        {
            EasingMode.EaseIn => EaseInQuadratic(t),
            EasingMode.EaseOut => EaseOutQuadratic(t),
            EasingMode.EaseInOut => EaseInOutQuadratic(t),
            _ => t
        };
    }

    Quaternion GetTargetRotation(float t)
    {
        if (Alignment == AlignmentMode.None) return m_InitialRotation;

        Vector3 forward = Vector3.forward;
        Vector3 up = Vector3.up;

        switch (Alignment)
        {
            case AlignmentMode.SplineElement:
                forward = Container.EvaluateTangent(m_SplinePath, t);
                up = Container.EvaluateUpVector(m_SplinePath, t);
                break;

            case AlignmentMode.SplineObject:
                forward = Container.transform.forward;
                up = Container.transform.up;
                break;

            case AlignmentMode.World:
                forward = Vector3.forward;
                up = Vector3.up;
                break;
        }

        var axisRemap = Quaternion.Inverse(Quaternion.LookRotation(
            GetAxisVector(ObjectForwardAxis),
            GetAxisVector(ObjectUpAxis)
        ));

        return Quaternion.LookRotation(forward, up) * axisRemap;
    }

    Vector3 GetAxisVector(AlignAxis axis)
    {
        return axis switch
        {
            AlignAxis.XAxis => Vector3.right,
            AlignAxis.YAxis => Vector3.up,
            AlignAxis.ZAxis => Vector3.forward,
            AlignAxis.NegativeXAxis => Vector3.left,
            AlignAxis.NegativeYAxis => Vector3.down,
            AlignAxis.NegativeZAxis => Vector3.back,
            _ => Vector3.forward
        };
    }

    // Public controls
    public void Play() => m_Playing = true;
    public void Pause() => m_Playing = false;
    public void Restart()
    {
        m_CurrentDistance = StartOffset * m_TotalLength;
        Speed = originalSpeed;
        Play();
    }

    public void StartDeceleration()
    {
        if (!UseDecelerationTrigger || isDecelerating) return;

        isDecelerating = true;
        StartCoroutine(DecelerateRoutine());
    }

    System.Collections.IEnumerator DecelerateRoutine()
    {
        Debug.Log("Stopped at station");

        float duration = 2f;
        float time = 0f;
        float startSpeed = Speed;

        while (time < duration)
        {
            Speed = Mathf.Lerp(startSpeed, MinSpeed, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        Speed = MinSpeed;

        if (StopAtStation)
        {
            Pause();
            yield return new WaitForSeconds(WaitTimeAtStation);

            if (ReverseAfterStation)
            {
                m_CurrentDistance -= 0.1f; // small offset to reverse
                Speed = originalSpeed;
                StartCoroutine(ReverseRoutine());
            }
            else
            {
                Speed = originalSpeed;
                Play();
            }
        }
    }

    System.Collections.IEnumerator ReverseRoutine()
    {
        float reverseSpeed = originalSpeed;
        while (m_CurrentDistance > 0f)
        {
            m_CurrentDistance -= reverseSpeed * Time.deltaTime;
            m_CurrentDistance = Mathf.Max(0f, m_CurrentDistance);
            UpdateTransformByDistance();
            yield return null;
        }

        Completed?.Invoke();
        Pause();
    }


    // Easing functions
    float EaseInQuadratic(float t) => t * t;
    float EaseOutQuadratic(float t) => t * (2f - t);
    float EaseInOutQuadratic(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    public enum LoopMode { Once, Loop, PingPong }
    public enum EasingMode { None, EaseIn, EaseOut, EaseInOut }
    public enum AlignmentMode { None, SplineElement, SplineObject, World }
    public enum AlignAxis { XAxis, YAxis, ZAxis, NegativeXAxis, NegativeYAxis, NegativeZAxis }
}
