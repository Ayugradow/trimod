using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriMod.Redwing
{
    public class pillardetect : MonoBehaviour
    {
        private List<GameObject> enemyList;
        private void Start()
        {
            enemyList = new List<GameObject>();
        }

        // From grimmchild upgrades and: Token: 0x0600006E RID: 110 RVA: 0x00005168 File Offset: 0x00003368
        public GameObject firePillarTarget()
        {
            GameObject result = null;
            float num = 99999f;
            if (enemyList.Count <= 0) return null;
            
            for (int i = enemyList.Count - 1; i > -1; i--)
            {
                if (enemyList[i] == null || !enemyList[i].activeSelf)
                {
                    enemyList.RemoveAt(i);
                }
            }
            foreach (GameObject enemyGameObject in enemyList)
            {
                // just pick enemy in range
                if (enemyGameObject == null) continue;
                float sqrMagnitude = (this.gameObject.transform.position - enemyGameObject.transform.position).sqrMagnitude;
                if (!(sqrMagnitude < num)) continue;
                result = enemyGameObject;
                num = sqrMagnitude;
            }
            return result;
        }
        
        private void OnTriggerEnter2D(Collider2D otherCollider)
        {
            if (otherCollider.gameObject.layer != 11) return;
            enemyList.Add(otherCollider.gameObject);
        }

        private void OnTriggerExit2D(Collider2D otherCollider)
        {
            if (otherCollider.gameObject.layer != 11) return;
            enemyList.Remove(otherCollider.gameObject);
        }

        public bool isEnemyInRange()
        {
            return enemyList.Count != 0;
        }
        
        private static void log(string str)
        {
            Modding.Logger.Log("[Redwing] " + str);
        }
    }
}