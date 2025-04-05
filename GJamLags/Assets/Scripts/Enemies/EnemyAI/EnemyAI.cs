using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyAI : MonoBehaviour
{
    //Estamos definiendo los posibles estados del enemigo
    public enum enemyState 
    {
        Patrol,
        Chase,
        Attack
    }

    #region variables
    [Header("Attack settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private BoxCollider2D attackHitbox;
    private float lastAttackTime;

    [Header("Patrol settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 3;
    private int currentWaypoint = 0;

    [Header("Chase settings")]
    [SerializeField] private float chaseSpeed = 4;
    [SerializeField] private float chaseDistance = 4;

    [Header("Referencias")]
    [SerializeField] private EnemyVision EnemyVision;

    [Header("Animaciones")]
    [SerializeField] private Animator animator;

    //Inicializamos el estado inicial en patrullaje
    private enemyState currentState = enemyState.Patrol;
    #endregion
    void Update()
    {
        //Evaluamos en cada frame el estado en el que se puede encontrar el enemigo
        switch (currentState) 
        {
            case enemyState.Patrol:
                Patrol();
                checkForPlayer();
                break;
            case enemyState.Chase:
                Chase();
                CheckAttackConditions();
                break;
            case enemyState.Attack:
                Attack();
                CheckIfPlayerPutOfRange();
                break;
        }
    }


    //Patrullaje del enemigo

    private void Attack() 
    {
        Debug.Log("Estado: ATAQUE - Iniciando ataque");
        transform.position = transform.position;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(PerformAttack());
            lastAttackTime = Time.time;
        }
    }

    private IEnumerator PerformAttack() 
    {
        Debug.Log("ATAQUE - activando hitbox de daño");
        attackHitbox.enabled = true;

        yield return new WaitForSeconds(0.2f);

        Debug.Log("ATAQUE - desactivando hitbox de daño");
        attackHitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0) 
        {
            Debug.Log("ATAQUE CONECTADO - jugador golpeado");
            //lógica del player health
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attackHitbox != null) 
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(attackHitbox.bounds.center, attackHitbox.bounds.size);
        }
    }

    private void CheckAttackConditions()
    {
        if (!EnemyVision.PlayerDetected || EnemyVision.Player == null) 
        {
            currentState = enemyState.Patrol;
        }

        float distance = Vector2.Distance(transform.position, EnemyVision.Player.position);
        if (distance <= attackRange) 
        {
            currentState = enemyState.Attack;
        }
    }

    private void CheckIfPlayerPutOfRange() 
    {
        if (EnemyVision.Player == null)
        {
            Debug.Log("Jugador perdido - volviendo a patrullar");
            currentState = enemyState.Patrol;
            return;
        }
    }
    private void Patrol() 
    {
        if (waypoints.Length == 0) return;//si no hay waypoint, entonces no camina

        Transform target = waypoints[currentWaypoint];//waypoint objetivo
        transform.position = Vector2.MoveTowards(transform.position,target.position,patrolSpeed * Time.deltaTime);//movimiento hacia el waypoint

        //Rotacion del patrullaje
        bool isMovingLeft = target.position.x < transform.position.x;
        transform.rotation = Quaternion.Euler(0, isMovingLeft ? 180 : 0,0);

        //actualizamos la posicion del waypoint
        if (Vector2.Distance(transform.position, target.position) < 0.1f) 
        {
            currentWaypoint = currentWaypoint + 1;
        }

        if (currentWaypoint == waypoints.Length) 
        {
            currentWaypoint = 0;
        }
        animator.SetFloat("Speed", target.position.sqrMagnitude);
    }


    //persecucion enemigo
    private void Chase() 
    {
        if (EnemyVision.Player == null) return;//Si no detecta ningun enemigo no lo persigue

        //movimiento hacia el jugador
        transform.position = Vector2.MoveTowards(transform.position, EnemyVision.Player.position, chaseSpeed * Time.deltaTime);

        //Rotacion durante la persecucion
        Vector2 direction = EnemyVision.Player.position - transform.position;
        //direction.Normalize();
        transform.rotation = Quaternion.Euler(0, direction.x < 0 ? 180 : 0, 0);

        animator.SetFloat("Speed", EnemyVision.Player.position.sqrMagnitude);
    }

    private void checkForPlayer() 
    {
        if (EnemyVision.PlayerDetected && EnemyVision.Player != null) 
        {
            currentState = enemyState.Chase;
        }
    }

    private void checkIfPlayerLost() 
    {
        if (!EnemyVision.PlayerDetected || EnemyVision.Player == null) 
        {
            currentState = enemyState.Patrol;
        }
    }
}
