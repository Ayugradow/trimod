using System;
using System.Collections;
using UnityEngine;

namespace TriMod.Redwing
{
    /// <summary>
    ///  The Ghost Knight is a fake knight that your knight can teleport to. All this code does is stop it from colliding
    /// </summary>
    public class ghostknight : MonoBehaviour
    {
        private Rigidbody2D rb;

        private void Start()
        {
            rb = gameObject.GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (rb.velocity.y > 0)
            {
                StartCoroutine(slashKeyDetect());
                StartCoroutine(moveWhileActive());
            }
        }

        private IEnumerator moveWhileActive()
        {
            while (rb.velocity.y > 0)
            {
                if (GameManager.instance.inputHandler.inputActions.left.State)
                {
                    rb.velocity = new Vector2(-10f, rb.velocity.y);
                } else if (GameManager.instance.inputHandler.inputActions.right.State)
                {
                    rb.velocity = new Vector2(10f, rb.velocity.y);
                }
                yield return null;
            }
            rb.velocity = Vector2.zero;
        }

        private IEnumerator slashKeyDetect()
        {
            while (GameManager.instance.inputHandler.inputActions.attack.State && rb.velocity.y > 0)
            {
                yield return null;
            }
            while (!GameManager.instance.inputHandler.inputActions.attack.State && rb.velocity.y > 0)
            {
                yield return null;
            }

            if (rb.velocity.y > 0)
            {
                rb.velocity = Vector2.zero;
            }

        }

        public void OnTriggerEnter2D(Collider2D hitbox)
        {
            int targetLayer = hitbox.gameObject.layer;
            if (targetLayer != 8) return;
            rb = gameObject.GetComponent<Rigidbody2D>();
            var transform1 = transform;
            Vector3 oldPos = transform1.position;
            transform1.position = new Vector3(oldPos.x, oldPos.y - 0.2f, oldPos.z);
            if (rb.velocity != Vector2.zero)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }
}