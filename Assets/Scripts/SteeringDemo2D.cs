using UnityEngine;

public class SteeringDemo2D : MonoBehaviour
{
    public GameObject characterPrefab;
    public GameObject targetPrefab;
    public GameObject hazardPrefab;

    private GameObject character;
    private GameObject target;
    private GameObject hazard;

    private Rigidbody2D rb;

    private enum Mode { None, Seek, Flee, Arrive, Avoid }
    private Mode currentMode = Mode.None;

    public float maxSpeed = 8f;
    public float arriveRadius = 2f;
    public float arriveSlowRadius = 4f;
    public float arriveStopRadius = 0.2f;
    public float minArriveSpeed = 0.3f;
    public float avoidForce = 10f;
    public float avoidLookAhead = 3f;
    public float avoidRadius = 1.5f;
    public float avoidStrength = 12f;
    public float seekOvershootDistance = 2.5f;
    public float seekThroughDistance = 6f;
    public float seekTurnSharpness = 1f;
    private bool fleeStopped = false;
    private float screenPadding = 0.5f;
    public AudioSource bgMusic;          // Background music AudioSource
    public AudioClip seekSFX;
    public AudioClip fleeSFX;
    public AudioClip arriveSFX;
    public AudioClip avoidSFX;
    public AudioClip resetSFX;


    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        if (bgMusic != null && !bgMusic.isPlaying)
            bgMusic.Play(); // Looping background music
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
            ResetScene();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            StartSeek();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            StartFlee();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            StartArrive();

        if (Input.GetKeyDown(KeyCode.Alpha4))
            StartAvoid();
    }

    void FixedUpdate()
    {
        if (!character) return;

        Vector2 steering = Vector2.zero;

        switch (currentMode)
        {
            case Mode.Seek:
                // Compute direction toward through-point
                steering = Seek((Vector2)target.transform.position);

                // Hard-set velocity to max speed in that direction
                rb.linearVelocity = steering * maxSpeed;
                break;

            case Mode.Flee:
                if (!fleeStopped)
                {
                    steering = Flee((Vector2)target.transform.position);
                    rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity + steering, maxSpeed);
                    StopAtScreenEdge();
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
                break;

            case Mode.Arrive:
                steering = Arrive((Vector2)target.transform.position);
                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity + steering, maxSpeed);
                break;

            case Mode.Avoid:
                steering = Seek((Vector2)target.transform.position);
                steering += Avoid((Vector2)hazard.transform.position);
                rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity + steering, maxSpeed);
                break;
        }

        if (rb.linearVelocity.magnitude > 0.1f)
            character.transform.up = rb.linearVelocity.normalized;
    }

    // ----------------- BEHAVIOURS -----------------

    Vector2 Seek(Vector2 targetPos)
    {
        Vector2 toTarget = targetPos - rb.position;

        // Far beyond the target so overshoot is visible
        Vector2 throughPoint = targetPos + toTarget.normalized * seekThroughDistance;

        return (throughPoint - rb.position).normalized;
    }

    Vector2 Flee(Vector2 threatPos)
    {
        Vector2 desired = (rb.position - threatPos).normalized * maxSpeed;
        return desired - rb.linearVelocity;
    }

    Vector2 Arrive(Vector2 targetPos)
    {
        Vector2 toTarget = targetPos - rb.position;
        float distance = toTarget.magnitude;

        // Hard stop when close enough
        if (distance <= arriveStopRadius)
        {
            rb.linearVelocity = Vector2.zero;
            return Vector2.zero;
        }

        // Scale speed based on distance (very visible slowdown)
        float rampedSpeed = maxSpeed * (distance / arriveSlowRadius);

        // Clamp to ensure noticeable crawling at the end
        float clampedSpeed = Mathf.Clamp(rampedSpeed, minArriveSpeed, maxSpeed);

        Vector2 desired = toTarget.normalized * clampedSpeed;
        return desired - rb.linearVelocity;
    }

    Vector2 Avoid(Vector2 hazardPos)
    {
        Vector2 forward = rb.linearVelocity.normalized;
        if (forward == Vector2.zero)
            forward = ((Vector2)target.transform.position - rb.position).normalized;

        // Look-ahead point in front of the character
        Vector2 ahead = rb.position + forward * avoidLookAhead;

        float distanceToHazard = Vector2.Distance(hazardPos, ahead);

        if (distanceToHazard > avoidRadius)
            return Vector2.zero;

        // Choose a strong perpendicular direction
        Vector2 sideStep = Vector2.Perpendicular(forward).normalized;

        // Push away from the hazard side
        if (Vector2.Dot(sideStep, hazardPos - rb.position) > 0)
            sideStep = -sideStep;

        return sideStep * avoidStrength;
    }

    // ----------------- HELPERS -----------------

    void StopAtScreenEdge()
    {
        if (fleeStopped) return;

        Vector3 viewPos = cam.WorldToViewportPoint(character.transform.position);

        bool hitEdge =
            viewPos.x <= 0 + screenPadding / cam.pixelWidth ||
            viewPos.x >= 1 - screenPadding / cam.pixelWidth ||
            viewPos.y <= 0 + screenPadding / cam.pixelHeight ||
            viewPos.y >= 1 - screenPadding / cam.pixelHeight;

        if (hitEdge)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            fleeStopped = true;
        }
    }

    public void ResetScene()
    {
        Destroy(character);
        Destroy(target);
        Destroy(hazard);

        currentMode = Mode.None;
        fleeStopped = false;

        if (resetSFX != null)
            bgMusic.PlayOneShot(resetSFX);
    }

    Vector2 RandomScreenPosition()
    {
        return cam.ViewportToWorldPoint(new Vector3(
            Random.Range(0.1f, 0.9f),
            Random.Range(0.1f, 0.9f),
            0));
    }

    void SpawnCharacter()
    {
        character = Instantiate(characterPrefab, RandomScreenPosition(), Quaternion.identity);
        rb = character.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
    }

    // ----------------- MODES -----------------

    void StartSeek()
    {
        ResetScene();
        SpawnCharacter();
        target = Instantiate(targetPrefab, RandomScreenPosition(), Quaternion.identity);
        currentMode = Mode.Seek;

        if (seekSFX != null)
            bgMusic.PlayOneShot(seekSFX);
    }

    void StartFlee()
    {
        ResetScene();
        SpawnCharacter();
        target = Instantiate(targetPrefab, RandomScreenPosition(), Quaternion.identity);
        currentMode = Mode.Flee;

        if (fleeSFX != null)
            bgMusic.PlayOneShot(fleeSFX);
    }

    void StartArrive()
    {
        ResetScene();
        SpawnCharacter();
        target = Instantiate(targetPrefab, RandomScreenPosition(), Quaternion.identity);
        currentMode = Mode.Arrive;

        if (arriveSFX != null)
            bgMusic.PlayOneShot(arriveSFX);
    }

    void StartAvoid()
    {
        ResetScene();
        SpawnCharacter();

        target = Instantiate(targetPrefab, RandomScreenPosition(), Quaternion.identity);

        // Place hazard BETWEEN character and target
        Vector2 charPos = character.transform.position;
        Vector2 targetPos = target.transform.position;

        Vector2 direction = (targetPos - charPos).normalized;
        float distance = Vector2.Distance(charPos, targetPos);

        Vector2 hazardPos = charPos + direction * (distance * 0.5f);

        hazard = Instantiate(hazardPrefab, hazardPos, Quaternion.identity);

        currentMode = Mode.Avoid;

        if (avoidSFX != null)
            bgMusic.PlayOneShot(avoidSFX);
    }

    void OnDrawGizmos()
    {
        if (!character) return;

        Gizmos.color = Color.white;

        switch (currentMode)
        {
            case Mode.Seek:
                if (target)
                {
                    Vector2 toTarget = (Vector2)target.transform.position - rb.position;
                    Vector2 throughPoint = (Vector2)target.transform.position + toTarget.normalized * seekThroughDistance;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(rb.position, throughPoint);
                    Gizmos.DrawSphere(throughPoint, 0.2f);
                }
                break;

            case Mode.Flee:
                if (target)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(rb.position, target.transform.position);
                    Gizmos.DrawSphere(target.transform.position, 0.2f);
                }
                break;

            case Mode.Arrive:
                if (target)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(rb.position, target.transform.position);
                    Gizmos.DrawWireSphere(target.transform.position, arriveSlowRadius);
                }
                break;

            case Mode.Avoid:
                if (target)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(rb.position, target.transform.position);
                }

                if (hazard)
                {
                    Vector2 forward = rb.linearVelocity.normalized;
                    if (forward == Vector2.zero)
                        forward = ((Vector2)target.transform.position - rb.position).normalized;

                    Vector2 ahead = rb.position + forward * avoidLookAhead;

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(rb.position, ahead);
                    Gizmos.DrawLine(ahead, hazard.transform.position);
                    Gizmos.DrawSphere(hazard.transform.position, 0.2f);
                }
                break;
        }
    }
}
