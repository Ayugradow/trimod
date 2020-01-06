using System;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using Modding;
using UnityEngine;
using Bounds = UnityEngine.Bounds;

namespace TriMod.Redwing
{
    public class Knight : MonoBehaviour
    {
        public static GameObject KnightGameObject;
        private tk2dSprite KnightSprite;
        private PlayMakerFSM ProxyFSM;
        private PlayMakerFSM SpellControl;
        private PlayMakerFSM NailArtControl;
        private GameObject PillarDetectionObject;
        public static pillardetect PillarDetection;
        private float flameStrength;
        readonly private double strengthPerSecond = 0.4;
        public const float FP_X_RANGE = 13;
        public const float FP_Y_RANGE = 6;

        private bool _isEnabled = false;

        private bool _isFocusing = false;

        private void Start()
        {
            StartCoroutine(getHeroFSMs());
        }

        public void EnableRedwing()
        {
            _isEnabled = true;
            textures.loadAllTextures();
            ModHooks.Instance.FocusCostHook += InstanceOnFocusCostHook;
            ModHooks.Instance.BeforeAddHealthHook += InstanceOnBeforeAddHealthHook;

            StartCoroutine(AddHeroHooks());
        }

        private int InstanceOnBeforeAddHealthHook(int amount)
        {
            if (amount < 2)
                return 0;
            return amount - 1;
        }

        public void FirePillar()
        {
            _isFocusing = false;
            objectspawner.SpawnFirePillar(flameStrength);
            Log("Doing a firepiller of strength " + flameStrength);
            
            
            // Firepiller code
            flameStrength = 0f;
        }

        public void FirePillarDamage()
        {
            if (!_isFocusing)
                return;
            flameStrength *= 0.6f;
            Log("Doing a damage firepiller of strength " + flameStrength);
            FirePillar();
        }
        
        public void FirePillarCompleted()
        {
            Log("Doing a completed firepiller of strength " + flameStrength);
            FirePillar();
        }

        private IEnumerator AddHeroHooks()
        {
            while (NailArtControl == null)
                yield return null;
            
            try
            {
                CallMethod firePillarOnRecover = new CallMethod
                {
                    behaviour = this,
                    methodName = "FirePillar",
                    parameters = new FsmVar[0],
                    everyFrame = false
                };
                AddActionFirst(SpellControl, "Focus Cancel", firePillarOnRecover);
                
                CallMethod firePillarOnDamage = new CallMethod
                {
                    behaviour = this,
                    methodName = "FirePillarDamage",
                    parameters = new FsmVar[0],
                    everyFrame = false
                };
                AddActionFirst(SpellControl, "Cancel All", firePillarOnDamage);
                
                CallMethod firePillarOnCompleted = new CallMethod
                {
                    behaviour = this,
                    methodName = "FirePillarCompleted",
                    parameters = new FsmVar[0],
                    everyFrame = false
                };
                AddActionFirst(SpellControl, "Focus Get Finish", firePillarOnCompleted);

                Log("Added all hero hooks.");
            } catch (Exception e)
            {
                Log("Unable to add method: error " + e);
            }
        }

        private float InstanceOnFocusCostHook()
        {
            //Focus cost checked means that focus was called.
            Log("Started focus.");
            _isFocusing = true;
            StartCoroutine(FocusingPower());
            return 5f;
        }

        private IEnumerator FocusingPower()
        {
            flameStrength = 0.0f;

            int debugTestVal = 0;
            while (flameStrength < 1.0f && _isFocusing)
            {
                yield return null;
                KnightSprite.color = new Color(1.0f, 1.0f - (flameStrength / 2.0f),1.0f - (flameStrength / 2.0f));
                flameStrength += (float) (strengthPerSecond * Time.deltaTime);
                if (flameStrength > 1.0f)
                {
                    flameStrength = 1.0f;
                }

                debugTestVal++;
                if (debugTestVal % 10 == 1)
                    Log("Flame strength is " + flameStrength);
            }

            while (_isFocusing)
            {
                KnightSprite.color = Color.red;
                yield return null;
            }
            KnightSprite.color = Color.white;
        }

        public void disableRedwing()
        {
            if (_isEnabled)
            {
                ModHooks.Instance.FocusCostHook -= InstanceOnFocusCostHook;
                _isEnabled = false;
            }
        }
        
        private IEnumerator getHeroFSMs()
        {
            while (GameManager.instance == null || HeroController.instance == null)
                yield return null;

            KnightGameObject = GameObject.Find("Knight");
            KnightSprite = KnightGameObject.GetComponent<tk2dSprite>();
            ProxyFSM = FSMUtility.LocateFSM(KnightGameObject, "ProxyFSM");
            SpellControl = FSMUtility.LocateFSM(KnightGameObject, "Spell Control");
            NailArtControl = FSMUtility.LocateFSM(KnightGameObject, "Nail Arts");
            setupFlamePillar();
            
            Modding.Logger.LogDebug("Found Spell control and nail art control FSMs");
            //Knight.PrintSceneHierarchyTree("knight.txt");
        }
        
        private void setupFlamePillar()
        {            
            PillarDetectionObject = new GameObject("redwingFlamePillarDetect",
                typeof(pillardetect), typeof(Rigidbody2D), typeof(BoxCollider2D));
            PillarDetectionObject.transform.parent = KnightGameObject.transform;
            PillarDetectionObject.transform.localPosition = Vector3.zero;
            
            
            BoxCollider2D fpRangeCollide = PillarDetectionObject.GetComponent<BoxCollider2D>();
            Bounds bounds = fpRangeCollide.bounds;
            bounds.center = PillarDetectionObject.transform.position;
            fpRangeCollide.isTrigger = true;
            fpRangeCollide.size = new Vector2(FP_X_RANGE, FP_Y_RANGE);            

            Rigidbody2D fpFakePhysics = PillarDetectionObject.GetComponent<Rigidbody2D>();
            fpFakePhysics.isKinematic = true;
            PillarDetection = PillarDetectionObject.GetComponent<pillardetect>();
            
            Log("Added Flamepillar detection");
        }
        
        private static void AddAction(PlayMakerFSM fsm, string stateName, FsmStateAction action)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                FsmStateAction[] actions = t.Actions;

                Array.Resize(ref actions, actions.Length + 1);
                actions[actions.Length - 1] = action;

                t.Actions = actions;
            }
        }

        private static void AddActionFirst(PlayMakerFSM fsm, string stateName, FsmStateAction action)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                FsmStateAction[] actions = t.Actions;
                FsmStateAction[] actionsNew = t.Actions;
                
                Array.Resize(ref actions, actions.Length + 1);
                actions[0] = action;
                for (int i = 1; i < actions.Length; i++)
                {
                    actions[i] = actionsNew[i - 1];
                }
                t.Actions = actions;
            }
        }

        public static void Log(string message)
        {
            Modding.Logger.Log("[Trimod:Redwing] : " + message);
        }
    }
}