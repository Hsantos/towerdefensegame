﻿using System.Collections.Generic;
using Assets.Script.view.common;
using Assets.Script.view.mainspot;
using Assets.Script.view.statics;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Script.view.enemy
{
    public class EnemyView: CharacterView
    {
        [SerializeField] private EnemyType enemyType;
        [SerializeField] private EnemyAnimationView animationView;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private AttackView enemyAttack;
        [SerializeField] private float speed;
        [SerializeField] private int damage;
        [SerializeField] private float attackDelay;
        [SerializeField] private float distanceToAttack;

        private float minSpeed = 0.2f;
        public float Speed => speed;

        public EnemyType EnemyType => enemyType;

        private MainSpotView mainSpot;
        private bool enabledToAttack;

        public delegate void EnemyEvent();
        public static event EnemyEvent NotifyEnemyDied;

        protected override List<string> DamageTags => new List<string> { Gametag.CONSTRUCTION_ATTACK };

        protected override void Awake()
        {
            base.Awake();
            GameFlow.NotifyWin += EndGame;
            GameFlow.NotifyLose += EndGame;
        }

        private void EndGame()
        {
            navMeshAgent.speed = 0;
            navMeshAgent.isStopped = true;
            animationView.Stop();
            CancelInvoke(nameof(Attack));
        }

        protected override void OnDamage(GameObject other)
        {
            AttackView currentAttack = other.GetComponent<AttackView>();

            if (currentAttack.DamageAffected > 0)
            {
                life += -currentAttack.DamageAffected;
                lifeBar.UpdateStatus(-currentAttack.DamageAffected);
            }

            if (currentAttack.SpeedAffected > 0)
            {
                navMeshAgent.speed -= currentAttack.SpeedAffected;
                if (navMeshAgent.speed < minSpeed) navMeshAgent.speed = minSpeed;
            }

            Destroy(other.gameObject);
           
            if (life == 0)
            {
                NotifyEnemyDied?.Invoke();
                Destroy(gameObject);
            }
        }

        public EnemyView StartEnemy(MainSpotView mainSpot)
        {
            this.mainSpot = mainSpot;
            navMeshAgent.speed = speed;
            navMeshAgent.destination = mainSpot.RectBounds.position;
            lifeBar.SetStatus(life);

            return this;
        }

        void Update()
        {
            if (enabledToAttack) return;

            if (navMeshAgent.remainingDistance <= distanceToAttack)
            {
                animationView.Stop();
                navMeshAgent.isStopped = true;
                enabledToAttack = true;
                ActivateAttackLoop();
            }
        }

        private void ActivateAttackLoop()
        {
            InvokeRepeating(nameof(Attack), attackDelay,attackDelay);
        }

        private void Attack()
        {
            animationView.Attack();
            AttackView attack = Instantiate(enemyAttack, transform.position, transform.rotation);
            Vector3 direction = (mainSpot.transform.position - transform.position).normalized;
            attack.ShootDirection(0,direction, damage, 0);
        }

        void OnDestroy()
        {
            GameFlow.NotifyWin -= EndGame;
            GameFlow.NotifyLose -= EndGame;
            CancelInvoke(nameof(Attack));
        }

       
    }
}
