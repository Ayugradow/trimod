using System;
using System.Collections;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using Bounds = UnityEngine.Bounds;

namespace TriMod.Redwing
{
    public class Knight : MonoBehaviour
    {
        public static GameObject KnightGameObject;
        public static pillardetect PillarDetection;

        private GameObject GhostKnight;
        private GameObject CeilingDetectionObject;
        private ceilingdetect _ceilingdetect;
        private SpriteRenderer GhostSprite;
        private Rigidbody2D GhostPhysics;
        private Texture2D GhostImage;
        private tk2dSprite KnightSprite;
        private Rigidbody2D KnightPhysics;
        private PlayMakerFSM ProxyFSM;
        private PlayMakerFSM SpellControl;
        private PlayMakerFSM NailArtControl;
        private GameObject PillarDetectionObject;
        private GameObject FireBar;
        private GameObject canvasObj;
        private Image FireBarImage;
        private double firePower;
        private float flameStrength;
        readonly private double strengthPerSecond = 0.4;
        public const float FP_X_RANGE = 13;
        public const float FP_Y_RANGE = 6;

        private bool _isEnabled = false;
        private bool _isFocusing = false;
        private bool _isImmortal = false;

        private void Start()
        {
            StartCoroutine(getHeroFSMs());
        }

        private void OnDestroy()
        {
            Destroy(PillarDetectionObject);
            Destroy(FireBar);
            Destroy(canvasObj);
            Destroy(GhostKnight);
            Destroy(CeilingDetectionObject);
        }

        public void EnableRedwing()
        {
            _isEnabled = true;
            textures.loadAllTextures();
            ModHooks.Instance.FocusCostHook += InstanceOnFocusCostHook;
            ModHooks.Instance.BeforeAddHealthHook += InstanceOnBeforeAddHealthHook;
            ModHooks.Instance.AttackHook += InstanceOnAttackHook;
            ModHooks.Instance.TakeDamageHook += ImmortalCheck;

            canvasObj = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920f, 1080f));
            FireBar = CanvasUtil.CreateImagePanel(canvasObj,
                Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f)),
                new CanvasUtil.RectData(new Vector2(300f, 100f), new Vector2(0.9f, 0.9f),
                    new Vector2(0.910f, 0.89f), new Vector2(0.910f, 0.89f)));
            DontDestroyOnLoad(canvasObj);
            DontDestroyOnLoad(FireBar);
            
            FireBarImage = FireBar.GetComponent<Image>();

            FireBarImage.preserveAspect = false;
            FireBarImage.type = Image.Type.Filled;
            FireBarImage.fillMethod = Image.FillMethod.Horizontal;
            FireBarImage.fillAmount = (float) firePower;
            
            StartCoroutine(AddHeroHooks());
        }

        private int ImmortalCheck(ref int hazardtype, int damage)
        {
            if (hazardtype == 1)
            {
                if (_isImmortal)
                {
                    return 0;
                }
            }
            return damage;
        }

        private void Update()
        {
            if (firePower < 1.0)
            {
                firePower += Time.deltaTime * 0.2;
                FireBarImage.fillAmount = (float) firePower;
            }
            else
            {
                firePower = 1.0;
                FireBarImage.fillAmount = 1.0f;
            }
        }

        private IEnumerator ImmortalFreezeKnight(double time, bool resetVelocity)
        {
            _isImmortal = true;
            Vector3 currentPos = KnightGameObject.transform.position;
            Vector2 currentVelocity = KnightPhysics.velocity;
            float currentGravity = KnightPhysics.gravityScale;
            KnightPhysics.gravityScale = 0f;
            while (time > 0.0)
            {
                KnightGameObject.transform.position = new Vector3(currentPos.x, KnightGameObject.transform.position.y, currentPos.z);
                //KnightGameObject.transform.position = currentPos;
                
                KnightPhysics.velocity = Vector2.zero;
                yield return null;
                time -= Time.deltaTime;
            }
            _isImmortal = false;
            KnightPhysics.velocity = resetVelocity ? currentVelocity : Vector2.zero;
            KnightPhysics.gravityScale = currentGravity;
        }

        private IEnumerator fadeKnight(double time, double returnTime)
        {
            double startTime = 0.0;
            while (startTime < time)
            {
                double alpha = 1 - startTime / time;
                if (alpha < 0.0)
                {
                    alpha = 0.0;
                }
                KnightSprite.color = new Color(1f, 1f, 1f, (float) alpha);
                yield return null;
                startTime += Time.deltaTime;
            }
            KnightSprite.color = Color.clear;
            while (startTime < returnTime)
            {
                yield return null;
                startTime += Time.deltaTime;
            }
            KnightSprite.color = Color.white;
        }

        private IEnumerator TeleportToGhost(double time)
        {
            HeroController.instance.acceptingInput = false;
            double startTime = 0;
            int currentFrame = 0;
            while (startTime < time)
            {
                if (currentFrame < (int) (textures.WarpSprites.Length * (startTime / time)))
                {
                    currentFrame++;
                    GhostImage = textures.WarpSprites[currentFrame];
                }
                startTime += Time.deltaTime;
                yield return null;
            }

            HeroController.instance.acceptingInput = true;
            GhostSprite.color = Color.clear;
            GhostImage = textures.WarpSprites[0];
            GhostPhysics.velocity = Vector2.zero;
            KnightGameObject.transform.position = GhostKnight.transform.position;
            
        }
        

        private void InstanceOnAttackHook(AttackDirection dir)
        {
            if (dir == AttackDirection.upward && HeroController.instance.hero_state == ActorStates.airborne && firePower > 0.25 && !_isImmortal)
            {
                firePower -= 0.25;
                StartCoroutine(ImmortalFreezeKnight(0.7, false));
                GhostKnight.transform.position = KnightGameObject.transform.position;
                GhostPhysics.velocity = _ceilingdetect.hasCelingAbove() ? Vector2.zero : Vector2.up * 15f;
                GhostSprite.color = Color.white;
                StartCoroutine(TeleportToGhost(0.7));
                StartCoroutine(fadeKnight(0.4, 0.7));

                Log("You're airborne and you did an upslash enjoy your cool teleport power");
            }
            Log("Player attacked with direction " + dir);
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
                Destroy(PillarDetectionObject);
                Destroy(FireBar);
                Destroy(canvasObj);
                Destroy(GhostKnight);
                Destroy(CeilingDetectionObject);
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
            KnightPhysics = KnightGameObject.GetComponent<Rigidbody2D>();
            setupFlamePillar();
            GhostKnight = new GameObject("GhostKnight", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(ghostknight));
            GhostKnight.layer = 0;
            BoxCollider2D gkCollide = GhostKnight.GetComponent<BoxCollider2D>();
            BoxCollider2D knightCollide = KnightGameObject.GetComponent<BoxCollider2D>();
            gkCollide.size = new Vector2(knightCollide.size.x + 0.4f, knightCollide.size.y + 1.2f);
            gkCollide.autoTiling = knightCollide.autoTiling;
            gkCollide.edgeRadius = knightCollide.edgeRadius;
            gkCollide.offset = new Vector2(knightCollide.offset.x, knightCollide.offset.y + 1.2f);
            gkCollide.isTrigger = true;
            GhostImage = GhostKnight.GetComponent<Texture2D>();
            GhostSprite = GhostKnight.GetComponent<SpriteRenderer>();
            GhostSprite.sprite = Sprite.Create(textures.WarpSprites[0], new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f), 400);
            GhostSprite.color = Color.clear;
            GhostPhysics = GhostKnight.GetComponent<Rigidbody2D>();
            GhostPhysics.drag = 0;
            GhostPhysics.gravityScale = 0f;
            GhostPhysics.simulated = true;
            GhostPhysics.isKinematic = true;
            GhostPhysics.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            CeilingDetectionObject = new GameObject("CeilingDetect", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(ceilingdetect));
            CeilingDetectionObject.transform.parent = KnightGameObject.transform;
            CeilingDetectionObject.transform.localPosition = new Vector3(0, 1.5f, 0);
            _ceilingdetect = CeilingDetectionObject.GetComponent<ceilingdetect>();
            BoxCollider2D cdCollider2D = CeilingDetectionObject.GetComponent<BoxCollider2D>();
            cdCollider2D.size = new Vector2(1f, 3f);
            Bounds bounds = cdCollider2D.bounds;
            bounds.center = CeilingDetectionObject.transform.position;
            cdCollider2D.isTrigger = true;
            Rigidbody2D cdFakePhysics = CeilingDetectionObject.GetComponent<Rigidbody2D>();
            cdFakePhysics.isKinematic = true;

            GhostKnight.layer = 0;
            DontDestroyOnLoad(GhostKnight);
            DontDestroyOnLoad(CeilingDetectionObject);
            Modding.Logger.LogDebug("Found Spell control and nail art control FSMs");
            //KnightGameObject.PrintSceneHierarchyTree("knight.txt");
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