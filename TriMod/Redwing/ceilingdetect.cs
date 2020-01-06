using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriMod.Redwing
{
    public class ceilingdetect : MonoBehaviour
    {
        private List<GameObject> celingList;
        private void Start()
        {
            celingList = new List<GameObject>();
        }

        private void OnTriggerEnter2D(Collider2D otherCollider)
        {
            if (otherCollider.gameObject.layer != 8) return;
            celingList.Add(otherCollider.gameObject);
        }

        private void OnTriggerExit2D(Collider2D otherCollider)
        {
            if (otherCollider.gameObject.layer != 8) return;
            celingList.Remove(otherCollider.gameObject);
        }

        public bool hasCelingAbove()
        {
            return celingList.Count != 0;
        }
    }
}