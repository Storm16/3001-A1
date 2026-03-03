using UnityEngine;

public class MovingHazard2D : MonoBehaviour
{
    public Vector2 moveDirection = Vector2.up;
    public float speed = 2f;
    public float moveDistance = 2f;

    private Vector2 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * moveDistance;
        transform.position = startPos + moveDirection.normalized * offset;
    }
}
