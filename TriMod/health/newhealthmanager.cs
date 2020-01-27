using System;
using System.Collections;
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
        private void Start()
        {
            ModHooks.Instance.OnEnableEnemyHook += InstanceOnOnEnableEnemyHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += LoadNewScene;
            On.HealthManager.Hit += HealthManagerOnHit;
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
                    go.AddComponent<newhealth>();
                }
            }
        }
        private void LoadNewScene(Scene arg0, Scene arg1)
        {
            StartCoroutine(scanEnemies());
        }

        private bool InstanceOnOnEnableEnemyHook(GameObject enemy, bool isalreadydead)
        {
            Redwing.Knight.Log("Enemy detected " + enemy.name);
            if (enemy.GetComponent<HealthManager>() != null && enemy.GetComponent<newhealth>() == null)
            {
                enemy.AddComponent<newhealth>();
            }

            return false;
        }
    }
}