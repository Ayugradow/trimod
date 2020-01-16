using System;
using UnityEngine;

namespace TriMod.Redwing
{
    public class laserControl : MonoBehaviour
    {
        private void Start()
        {
            if (lr == null)
            {
                lr = GetComponent<LineRenderer>();
            }
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = new Color(1f, 0f, 0f, 0.6f);
        }

        public LineRenderer lr;
        public void DrawRay()
        {
            lr.startWidth = 0.3f;
            lr.endWidth = 0.3f;
            // Bit shift the index of the layer (8) to get a bit mask
            int layerMask = 1 << 8;
            var position = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(position.x, position.y), Vector2.down, Mathf.Infinity, layerMask);
            if (hit)
            {
                var position1 = transform.position;
                Vector3[] positions = new[] {position1, position1 + hit.distance * Vector3.down};
                lr.SetPositions(positions);
                //Knight.Log("Did Hit " + hit.distance + " normal is " + hit.normal);
            }
            else
            {
                var position1 = transform.position;
                Vector3[] positions = new[] {position1, position1 + 100f * Vector3.down};
                lr.SetPositions(positions);
                lr.colorGradient = new Gradient();
                Knight.Log("Did not Hit");
            }
        }

        public void UndrawRay()
        {
            Vector3[] positions = new[] {Vector3.down * 10000f, Vector3.down * 10000f};
            lr.SetPositions(positions);
        }
    }
}