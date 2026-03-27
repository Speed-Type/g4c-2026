using System.Collections.Generic;
using UnityEngine;

public class SnakeEnemy : MonoBehaviour
{
    //Public variables

    [Header("References")]
    public SnakeGameplayController snakeController;

    [Header("Movement")]
    [Tooltip("How fast the opponent moves")]
    public float moveSpeed = 3.5f;
    [Tooltip("How fast the opponent can turn (degrees per sec)")]
    public float turnSpeed = 90f;
    [Tooltip("Wander circle distance; higher = smoother wander arc")]
    public float wanderDistance = 2f;
    [Tooltip("Wander circle radius; higher = more erratic wander")]
    public float wanderRadius = 1.2f;
    [Tooltip("How much the wander angle drifts each second (degrees)")]
    public float wanderJitter = 40f;

    [Header("Flee")]
    [Tooltip("Opponent starts fleeing when the nearest body segment is within this range")]
    public float fleeRadius = 5f;
    [Tooltip("Weight of the flee force relative to wander")]
    public float fleeWeight = 2.5f;

    [Header("Wall avoidance")]
    [Tooltip("Centre of the arena in world space")]
    public Vector3 arenaCenter = Vector3.zero;
    [Tooltip("Half-extents of the arena (X and Z); Y is ignored because opponent is flat")]
    public Vector3 arenaHalfExtents = new Vector3(20f, 0f, 20f);
    [Tooltip("Distance from wall at which avoidance kicks in")]
    public float wallAvoidanceMargin = 4f;
    [Tooltip("Weight of wall avoidance force")]
    public float wallAvoidanceWeight = 3f;

    [Header("Collision detection")]
    [Tooltip("Radius for detecting contact with snake body segments")]
    public float contactRadius = 0.6f;

    //----------------------------------------------------------------------------------------------------------------

    //PRIVATE VARIABLES

    private float groundY;
    private Vector3 heading; //current normalised movement direction
    private float wanderAngle; //current wander angle offset (degrees)
    private bool isActive = true;

    //Cache segment transforms each frame to avoid repeated GetComponent calls.
    //SnakeController exposes segments as a List<GameObject> — we read it via the public accessor added below
    private List<GameObject> segments;

    //----------------------------------------------------------------------------------------------------------------

    //Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Flee radius
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, fleeRadius);

        // Contact radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactRadius);

        // Arena bounds
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireCube(arenaCenter, arenaHalfExtents * 2f);

        // Heading
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, heading * 2f);
    }
#endif
}
