using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace TriMod.Redwing
{
    public class firepillar : MonoBehaviour
    {
        public float _lifespan;
        public float _strength;
        private float _currentLifetime = 0;
        private Texture2D _selfTexture;
        private int _currentTexture = 0;
        private const int MaxTexture = 4;
        
        private void Start()
        {
            Knight.Log("Created a firepillar with lifespan " + _lifespan + " and strength " + _strength);
            Knight.Log("Current location is " + gameObject.transform.GetPositionX() + ", " + gameObject.transform.GetPositionY());
            StartCoroutine(Animate());
            _selfTexture = gameObject.GetComponent<Texture2D>();
        }

        private IEnumerator Animate()
        {
            while (_currentLifetime < _lifespan)
            {
                if (_currentTexture < (int) (MaxTexture * (_currentLifetime / _lifespan)))
                {
                    _currentTexture++;
                    _selfTexture = textures.FocusBeam[_currentTexture];
                }
                yield return null;
                _currentLifetime += Time.deltaTime;
            }
            Destroy(gameObject);
        }
    }
}