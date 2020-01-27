using System;
using UnityEngine;

namespace TriMod.health
{
    public class newhealth : MonoBehaviour
    {
        private HealthManager connectedHM;
        public double trueHealth;
        public float damageTakenTimer = 0f;

        private void Update()
        {
            damageTakenTimer -= Time.deltaTime;
        }


        private void Start()
        {
            connectedHM = gameObject.GetComponent<HealthManager>();
            trueHealth = connectedHM.hp;
        }

        public static HitInstance generateHitInstance(GameObject source, double damage, bool ignoreInvuln = false, int damageType = 1, float direction = 5f, bool raddirection = false)
        {
            HitInstance hi;
            hi.Direction = direction;
            hi.Multiplier = 1f;
            hi.Source = source;
            hi.DamageDealt = (int) damage;
            hi.IsExtraDamage = false;
            hi.IgnoreInvulnerable = ignoreInvuln;
            if (damageType < 8 && damageType >= 0)
                hi.AttackType = (AttackTypes) damageType;
            else
                hi.AttackType = AttackTypes.Generic;
            hi.CircleDirection = raddirection;
            hi.MagnitudeMultiplier = 1f;
            hi.SpecialType = SpecialTypes.None;
            hi.MoveAngle = 0f;
            hi.MoveDirection = false;
            return hi;
        }

        public bool trueTakeDamage(double trueDamage, int damageType, HitInstance ogHit)
        {
            AttackTypes at;
            if (damageType < 8 && damageType > 0)
            {
                at = (AttackTypes) damageType;
            }
            else
            {
                at = AttackTypes.Spell;
            }

            bool invuln = connectedHM.IsBlockingByDirection((int)ogHit.Direction, at);
            if (invuln && !ogHit.IgnoreInvulnerable)
            {
                return false;
            }

            trueHealth -= trueDamage;
            if (trueHealth < 0)
            {
                connectedHM.Die(ogHit.Direction, at, true);
                return true;
            }
            
            if (connectedHM.hp > (int) (trueHealth + 1.0) && damageTakenTimer < 0f)
            {
                ogHit.DamageDealt = (int) (connectedHM.hp - trueHealth);
                connectedHM.Hit(ogHit);
                damageTakenTimer = 0.2f;
            }
            // In this special case the user actually healed the target.
            else if (connectedHM.hp < (int) (trueHealth))
            {
                connectedHM.hp = (int) trueHealth;
            }




            return true;
        }
    }
}