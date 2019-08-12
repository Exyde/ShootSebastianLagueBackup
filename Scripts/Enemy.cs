using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof(NavMeshAgent))]
public class Enemy : LivingEntity
{

    public enum State {Idle, Chasing, Attacking};
    State currentState;
     
    NavMeshAgent pathfinder;
    Transform target;
    LivingEntity targetEntity;
    Material skinMaterial;

    Color originalColor;

    float attackDistanceThreshold = .5f;
    float timeBetweenAttack = 1;
    float nextAttackTime;
    float damage = 1;

    float myCollisionRadius;
    float targetCollisionRadius;

    bool hasTarget;

    protected override void Start()
    {
        base.Start();
        pathfinder = GetComponent<NavMeshAgent>();
        skinMaterial = GetComponent<Renderer>().material;
        originalColor = skinMaterial.color;

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            hasTarget = true;
            currentState = State.Chasing;
            target = GameObject.FindGameObjectWithTag("Player").transform;
            targetEntity = target.gameObject.GetComponent<LivingEntity>();
            targetEntity.OnDeath += OnTargetDeath;

            myCollisionRadius = GetComponent<CapsuleCollider>().radius;
            targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;


            StartCoroutine(UpdatePath());
        }
    }

    void OnTargetDeath()
    {
        hasTarget = false;
        currentState = State.Idle;
    }

    void Update()
    {

        if (hasTarget)
        {
            if (Time.time > nextAttackTime)
            {
                float sqrDstToTarget = (target.position - transform.position).sqrMagnitude;

                if (sqrDstToTarget < Mathf.Pow(attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2))
                {
                    nextAttackTime = Time.time + timeBetweenAttack;
                    StartCoroutine(Attack());

                }
            }
        }


    }

    IEnumerator Attack()
    {
        currentState = State.Attacking;
        pathfinder.enabled = false;
        skinMaterial.color = Color.red;
         
        Vector3 originalPosition = transform.position;
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - dirToTarget * myCollisionRadius;

        bool hasAppliedDamaged = false;

        float percent = 0f;
        float attackSpeed = 3;

        while (percent <= 1)
        {
            if (percent >= .5f && !hasAppliedDamaged)
            {
                hasAppliedDamaged = true;
                targetEntity.TakeDamage(damage);
            }
            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2)  + percent ) * 4; // Parrabola ! :(
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }

        skinMaterial.color = originalColor;
        currentState = State.Chasing;
        pathfinder.enabled = true;
    }

    IEnumerator UpdatePath()
    {
        float refreshRate = .25f;

        while (hasTarget){
            if (currentState == State.Chasing){
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold / 2);
                if (!dead){
                    pathfinder.SetDestination(targetPosition);
                }
            }
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
