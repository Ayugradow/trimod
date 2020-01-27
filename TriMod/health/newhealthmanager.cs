using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnums;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TriMod.health
{
    public class newhealthmanager : MonoBehaviour
    {
        public static List<newhealth> enemyHealths = new List<newhealth>();
        private void Start()
        {
            ModHooks.Instance.OnEnableEnemyHook += InstanceOnOnEnableEnemyHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += LoadNewScene;
            On.HealthManager.Hit += HealthManagerOnHit;
        }

        public static void hurtAllEnemies(GameObject source, double damage)
        {
            foreach (newhealth nh in enemyHealths)
            {
                if (nh != null)
                {
                    Redwing.Knight.Log("[DEBUG] Hurting enemy " + source.name +
                                       " because hurt all enemies button pressed.");
                    CustomEnemyHit(nh.gameObject, source, damage);
                }
            }
        }
        public static void CustomEnemyHit(GameObject target, HitInstance hi, double damage = -1.0)
        {
            if (target.GetComponent<HealthManager>() == null)
            {
                Redwing.Knight.Log("Cannot target enemy, no hm for enemy named " + target.name);
                return;
            }

            newhealth nh = target.GetComponent<newhealth>();
            if (nh == null)
            {
                Redwing.Knight.Log("ERROR: No new healthmanager found on enemy with health manager " + target.gameObject.name);
                Redwing.Knight.Log("Please report to the mod author. Also adding one now...");
                target.gameObject.GetOrAddComponent<newhealth>();
                return;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (damage == -1.0)
            {
                damage = (double) (hi.DamageDealt) * hi.Multiplier;
            }
            nh.trueTakeDamage(damage, 10, hi, false);
        }

        public static void CustomEnemyHit(GameObject target, GameObject source, double damage)
        {
            HitInstance hi = newhealth.generateHitInstance(source, damage);
            CustomEnemyHit(target, hi, damage);
        }

        private void HealthManagerOnHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitinstance)
        {
            // Mutex lock checking.
            if (newhealth.runningNewHMOH)
            {
                orig(self, hitinstance);
                return;
            }
            newhealth nh = self.gameObject.GetComponent<newhealth>();
            if (nh == null)
            {
                Redwing.Knight.Log("ERROR: No new healthmanager found on enemy with health manager " + self.gameObject.name);
                Redwing.Knight.Log("Please report to the mod author. Also adding one now...");
                self.gameObject.GetOrAddComponent<newhealth>();
                orig(self, hitinstance);
                return;
            }

            bool b = nh.trueTakeDamage( ((double) hitinstance.DamageDealt) * hitinstance.Multiplier,
                (int) hitinstance.AttackType, hitinstance, true);
            if (b) // make enemy immune
                nh.trueImmortalityTimer = 0.2f;
            else if (nh.trueImmortalityTimer <= 0f) // enemy blocked and wasn't immune
                nh.trueImmortalityTimer = 0.15f;
            if (nh.trueHealth <= 0)
            {
                enemyHealths.Remove(nh);
            }
        }

        private void OnDestroy()
        {
            ModHooks.Instance.OnEnableEnemyHook -= InstanceOnOnEnableEnemyHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= LoadNewScene;
        }

        private IEnumerator scanEnemies()
        {
            yield return new WaitForSceneLoadFinish();
            GameObject[] gos = (GameObject[]) Object.FindObjectsOfType(typeof(GameObject));
            foreach (GameObject go in gos)
            {
                if (go.GetComponent<HealthManager>() != null && go.GetComponent<newhealth>() == null)
                {
                    Redwing.Knight.Log("Initial scan found enemy " + go.name);
                    enemyHealths.Add(go.AddComponent<newhealth>());
                }
            }
        }
        private void LoadNewScene(Scene arg0, Scene arg1)
        {
            enemyHealths.Clear();
            StartCoroutine(scanEnemies());
        }

        private bool InstanceOnOnEnableEnemyHook(GameObject enemy, bool isalreadydead)
        {
            Redwing.Knight.Log("Enemy detected " + enemy.name);
            if (enemy.GetComponent<HealthManager>() != null && enemy.GetComponent<newhealth>() == null)
            {
                enemyHealths.Add(enemy.AddComponent<newhealth>());
            }

            return false;
        }
    }
}