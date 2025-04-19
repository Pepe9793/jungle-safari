using UnityEngine;

public class SimpleCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxForwardSpeed = 20f;
    public float maxReverseSpeed = 10f;
    public float turnSpeed = 100f;
    public float accelerationRate = 5f;
    public float decelerationRate = 5f;

    [Header("Wheel Settings")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;
    public float wheelRadius = 0.3f;

    [Header("SFX Settings")]
    public AudioSource idleAudioSource;
    public AudioSource accelAudioSource;
    public AudioClip idleClip;
    public AudioClip accelerationClip;
    public float minPitch = 0.8f;
    public float maxPitch = 2f;
    public float idleVolume = 1f;
    public float accelVolume = 1f;

    private float moveInputRaw;
    private float turnInputRaw;
    private float currentSpeed;
    private float currentTurnSpeed;
    private float targetSpeed;

    void Start()
    {
        // Setup Idle AudioSource
        if (idleAudioSource == null)
        {
            idleAudioSource = gameObject.AddComponent<AudioSource>();
        }
        idleAudioSource.clip = idleClip;
        idleAudioSource.loop = true;
        idleAudioSource.playOnAwake = false;
        idleAudioSource.volume = idleVolume;
        idleAudioSource.pitch = minPitch;
        idleAudioSource.Play();

        // Setup Acceleration AudioSource
        if (accelAudioSource == null)
        {
            accelAudioSource = gameObject.AddComponent<AudioSource>();
        }
        accelAudioSource.clip = accelerationClip;
        accelAudioSource.loop = true;
        accelAudioSource.playOnAwake = false;
        accelAudioSource.volume = 0f;
        accelAudioSource.pitch = minPitch;
        accelAudioSource.Play();
    }

    void Update()
    {
        moveInputRaw = Input.GetAxis("Vertical");
        turnInputRaw = Input.GetAxis("Horizontal");

        if (moveInputRaw > 0)
            targetSpeed = moveInputRaw * maxForwardSpeed;
        else if (moveInputRaw < 0)
            targetSpeed = moveInputRaw * maxReverseSpeed;
        else
            targetSpeed = 0f;

        float speedLerpRate = Mathf.Abs(moveInputRaw) > 0 ? accelerationRate : decelerationRate;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedLerpRate);

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        currentTurnSpeed = Mathf.Lerp(currentTurnSpeed, turnInputRaw * turnSpeed, Time.deltaTime * (turnInputRaw != 0 ? accelerationRate : decelerationRate));
        float turningMultiplier = currentSpeed >= 0f ? -1f : 1f;
        transform.Rotate(Vector3.up * currentTurnSpeed * turningMultiplier * Time.deltaTime);

        SpinWheels();
        UpdateEngineSFX();
    }

    void SpinWheels()
    {
        float distanceMoved = currentSpeed * Time.deltaTime;
        float rotationAngle = (distanceMoved / (2 * Mathf.PI * wheelRadius)) * 360f;

        Vector3 spinAxis = Vector3.up;

        if (frontLeftWheel) frontLeftWheel.Rotate(spinAxis, rotationAngle, Space.Self);
        if (frontRightWheel) frontRightWheel.Rotate(spinAxis, rotationAngle, Space.Self);
        if (rearLeftWheel) rearLeftWheel.Rotate(spinAxis, rotationAngle, Space.Self);
        if (rearRightWheel) rearRightWheel.Rotate(spinAxis, rotationAngle, Space.Self);
    }

    void UpdateEngineSFX()
    {
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxForwardSpeed);

        // Adjust pitch
        float pitch = Mathf.Lerp(minPitch, maxPitch, normalizedSpeed);
        idleAudioSource.pitch = pitch;
        accelAudioSource.pitch = pitch;

        // Crossfade volumes
        idleAudioSource.volume = Mathf.Lerp(idleVolume, 0f, normalizedSpeed);
        accelAudioSource.volume = Mathf.Lerp(0f, accelVolume, normalizedSpeed);
    }
}
