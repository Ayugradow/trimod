using System;
using UnityEngine;

namespace TriMod.Redwing
{
    public class objectspawner
    {
        private const int PillarWidth = 200;
        private const int PillarHeight = 500;
        private const float PillarLifespan = 1.4f;

        public static void SpawnFirePillar(float strength)
        {
            GameObject firePillar = new GameObject("redwingFlamePillar", typeof(firepillar),
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D));
            firePillar.transform.localScale = new Vector3(strength, strength, 1f);
            GameObject fireAtJerk = null;

            if (Knight.PillarDetection.isEnemyInRange())
            {
                try
                {
                    fireAtJerk = Knight.PillarDetection.firePillarTarget();
                }
                catch (Exception e)
                {
                    Knight.Log("Spawn fire pillar failed with error " + e);
                }
            }

            if (fireAtJerk != null)
            {
                firePillar.transform.parent = null;
                firePillar.transform.localPosition = Vector3.zero;
                Vector3 position = fireAtJerk.gameObject.transform.position;
                Vector3 pillarRelativePosition = new Vector3(
                    position.x,
                    Knight.KnightGameObject.transform.position.y,
                    position.z - 0.003f);
                firePillar.transform.position = pillarRelativePosition;
            }
            else
            {
                firePillar.transform.parent = Knight.KnightGameObject.transform;
                firePillar.transform.localPosition = Vector3.zero;
            }
            SpriteRenderer img = firePillar.GetComponent<SpriteRenderer>();
            Rect pillarSpriteRect = new Rect(0, 0,
                PillarWidth, PillarHeight);
            img.sprite = Sprite.Create(textures.FocusBeam[0], pillarSpriteRect,
                new Vector2(0.5f, 0.5f), 30f);
            img.color = new Color(1f, 1f, 1f, 0.5f + strength * 0.5f);
            //img.enabled = true;

            Rigidbody2D fakePhysics = firePillar.GetComponent<Rigidbody2D>();
            fakePhysics.isKinematic = true;
            BoxCollider2D hitEnemies = firePillar.GetComponent<BoxCollider2D>();
            hitEnemies.isTrigger = true;
            hitEnemies.size = img.size;
            hitEnemies.offset = new Vector2(0, 0);

            firepillar f = firePillar.GetComponent<firepillar>();
            f._lifespan = PillarLifespan;
            f._strength = strength;
            firePillar.SetActive(true);
        }
    }
    
    
}