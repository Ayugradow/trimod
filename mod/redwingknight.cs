using System;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using Modding;
using UnityEngine;

namespace TriMod
{
    public class RedwingKnight: MonoBehaviour
    {
        private GameObject Knight;
        private tk2dSprite KnightSprite;
        private PlayMakerFSM ProxyFSM;
        private PlayMakerFSM SpellControl;
        private PlayMakerFSM NailArtControl;
        private float flameStrength;
        readonly private double strengthPerSecond = 0.4;

        private bool _isEnabled = false;

        private bool _isFocusing = false;

        private void Start()
        {
            StartCoroutine(getHeroFSMs());
        }

        public void EnableRedwing()
        {
            _isEnabled = true;
            ModHooks.Instance.FocusCostHook += InstanceOnFocusCostHook;
            ModHooks.Instance.BeforeAddHealthHook += InstanceOnBeforeAddHealthHook;

            StartCoroutine(AddHeroHooks());
        }

        private int InstanceOnBeforeAddHealthHook(int amount)
        {
            if (amount != 2)
                return 0;
            return 1;
        }

        public void FirePillar()
        {
            _isFocusing = false;
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

            Knight = GameObject.Find("Knight");
            KnightSprite = Knight.GetComponent<tk2dSprite>();
            ProxyFSM = FSMUtility.LocateFSM(Knight, "ProxyFSM");
            SpellControl = FSMUtility.LocateFSM(Knight, "Spell Control");
            NailArtControl = FSMUtility.LocateFSM(Knight, "Nail Arts");
            Modding.Logger.LogDebug("Found Spell control and nail art control FSMs");
            Knight.PrintSceneHierarchyTree("knight.txt");
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

        private static void Log(string message)
        {
            Modding.Logger.Log("[Trimod:Redwing] : " + message);
        }
    }
}