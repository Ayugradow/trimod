using System;
using System.Collections;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
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
        private laserControl KnightLaser; // :)
        private ceilingdetect _ceilingdetect;
        private ceilingdetect _floordetect;
        private SpriteRenderer GhostSprite;
        private Rigidbody2D GhostPhysics;
        private Texture2D GhostImage;
        private tk2dSprite KnightSprite;
        private Rigidbody2D KnightPhysics;
        private PlayMakerFSM ProxyFSM;
        private PlayMakerFSM SpellControl;
        private PlayMakerFSM NailArtControl;
        private GameObject KnightLaserObject;
        private GameObject FloorDetectionObject;
        private GameObject CeilingDetectionObject;
        private GameObject PillarDetectionObject;
        private GameObject FireBar;
        private GameObject canvasObj;
        private Image FireBarImage;
        private double firePower;
        private float flameStrength;
        readonly private double strengthPerSecond = 0.4;
        public const float FP_X_RANGE = 13;
        public const float FP_Y_RANGE = 6;
        private float _maxfallspeed = -5f;
        private double ktimer = 0.5;


        private bool _knightShouldBeInvuln = false;
        private bool _jetpackCycle = false;
        private bool _startedJetpack = false;
        private bool _isUpSlashing = false;
        private bool _isEnabled = false;
        private bool _isFocusing = false;
        private bool _isRocketJumping = false;
        private bool _acceptingInput = true;

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
            Destroy(KnightLaserObject);
        }

        public void EnableRedwing()
        {
            _isEnabled = true;
            textures.loadAllTextures();
            ModHooks.Instance.FocusCostHook += InstanceOnFocusCostHook;
            ModHooks.Instance.BeforeAddHealthHook += InstanceOnBeforeAddHealthHook;
            ModHooks.Instance.AttackHook += InstanceOnAttackHook;
            ModHooks.Instance.CharmUpdateHook += RedwingLance;
            ModHooks.Instance.DashPressedHook += NoDashWhileHoldingUp;
            On.HeroController.JumpReleased += NoVelocityResetOnReleaseWithJetpack;
            On.NailSlash.StartSlash += NailSlashOnStartSlash;
            ModCommon.ModCommon.OnSpellHook += OverwriteSpells;
            On.HeroController.CancelHeroJump += DontCancelWithJetpack;
            On.HeroController.AffectedByGravity += AffectedByGravityIsShit;
            On.HeroController.Jump += RocketJumpIfRocketJumping;

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

        private void RocketJumpIfRocketJumping(On.HeroController.orig_Jump orig, HeroController self)
        {
            if (_isRocketJumping)
            {
                int js = ReflectionHelper.GetAttr<HeroController, int>(HeroController.instance,"jump_steps");
                int jps = ReflectionHelper.GetAttr<HeroController, int>(HeroController.instance,"jumped_steps");
                if (js <= self.JUMP_STEPS)
                {
                    var velocity = KnightPhysics.velocity;
                    velocity = !self.inAcid
                        ? new Vector2(velocity.x, self.JUMP_SPEED * 1.8f)
                        : new Vector2(velocity.x, self.JUMP_SPEED_UNDERWATER * 1.8f);
                    KnightPhysics.velocity = velocity;

                    js++;
                    jps++;
                    ReflectionHelper.SetAttr(HeroController.instance,"jump_steps", js);
                    ReflectionHelper.SetAttr(HeroController.instance,"jumped_steps", jps);
                    ReflectionHelper.SetAttr(HeroController.instance,"ledgeBufferSteps", 0);
                }
                else
                    orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void AffectedByGravityIsShit(On.HeroController.orig_AffectedByGravity orig, HeroController self, bool gravityapplies)
        {
            if (KnightPhysics != null && self != null)
                KnightPhysics.gravityScale = gravityapplies ? self.DEFAULT_GRAVITY : 0f;
            else
            {
                Log("Something has gone terribly wrong. Let's reset you. ");
                orig(HeroController.instance, gravityapplies); // yuck
            }

        }

        private void DontCancelWithJetpack(On.HeroController.orig_CancelHeroJump orig, HeroController self)
        {
            Vector2 rbvec = KnightPhysics.velocity;
            orig(self);
            if (_jetpackCycle)
            {
                KnightPhysics.velocity = rbvec;
            }
        }

        private IEnumerator Groundpound()
        {
            // Groundpound particles and animation go here
            HeroController.instance.SetDamageMode(1);
            HeroController.instance.acceptingInput = false;
            KnightLaser.DrawRay();
            StartCoroutine(ImmortalFreezeKnight(0.4, true));
            yield return new WaitForSeconds(0.4f);
            while (!_floordetect.hasCelingAbove())
            {
                HeroController.instance.SetDamageMode(1);
                KnightLaser.DrawRay();
                KnightPhysics.velocity = Vector2.down * 150f;
                if (KnightPhysics.gravityScale < 0.78f || HeroController.instance.cState.falling == false)
                {
                    KnightPhysics.gravityScale = 0.78f;
                    HeroController.instance.cState.falling = true;
                    HeroController.instance.CancelHeroJump();
                }
                yield return null;
            }
            KnightPhysics.velocity = Vector2.zero;
            KnightLaser.UndrawRay();
            SpellControl.Fsm.BroadcastEvent("HERO CAST SPELL", false);
            SpellControl.Fsm.BroadcastEvent("QUAKE FALL END", false);

            //QUAKE FALL END
            
            Log("You landed. Doing damage and stuff I guess");
            HeroController.instance.acceptingInput = true;
            _acceptingInput = true;
            if (GameManager.instance.inputHandler.inputActions.jump.State)
            {
                Log("Jump pressed when you landed so doing ROCKET JUMP!!!!");
                ReflectionHelper.SetAttr(HeroController.instance,"jumpQueueSteps", (int) 0);
                ReflectionHelper.SetAttr(HeroController.instance,"jumped_steps", (int) 0);
                ReflectionHelper.SetAttr(HeroController.instance,"jump_steps", (int) 5);
                HeroController.instance.cState.jumping = true;
                _isRocketJumping = true;

                StartCoroutine(RocketJumpEffects());
            }
            yield return new WaitForSeconds(0.5f);
            if (_knightShouldBeInvuln)
                yield break;
            HeroController.instance.SetDamageMode(0);
        }

        private IEnumerator RocketJumpEffects()
        {
            while (GameManager.instance.inputHandler.inputActions.jump.State && HeroController.instance.cState.jumping)
            {
                // TODO effects
                yield return null;
            }

            _isRocketJumping = false;
        }

        private bool OverwriteSpells(ModCommon.ModCommon.Spell s)
        {
            if (s == ModCommon.ModCommon.Spell.Quake && !_floordetect.hasCelingAbove() && _acceptingInput)
            {
                _acceptingInput = false;
                Log("Pling, pa!");
                StartCoroutine(Groundpound());
            }
            
            Log("The following spell was casted " + s);
            return false;
        }

        private void NoVelocityResetOnReleaseWithJetpack(On.HeroController.orig_JumpReleased orig, HeroController self)
        {
            //Log("NoVelocityResetOnReleaseWithJetpack run");
            Vector2 rbvec = KnightPhysics.velocity;
            orig(self);
            if (_jetpackCycle)
            {
                KnightPhysics.velocity = rbvec;
            }
        }

        private bool NoDashWhileHoldingUp()
        {
            _startedJetpack = (GameManager.instance.inputHandler.inputActions.up.State && HeroController.instance.hero_state == ActorStates.airborne && !GameManager.instance.inputHandler.inputActions.jump);
            return (GameManager.instance.inputHandler.inputActions.up.State && HeroController.instance.hero_state == ActorStates.airborne);
        }

        private void NailSlashOnStartSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            //Log("Nail slash on start slash run...");
            //NailSlash
            if (_isUpSlashing)
            {
                self.scale.x = 0.9f;
                self.scale.y = 2.2f;
            }
            else
            {
                self.scale.x = 2.2f;
                self.scale.y = 0.9f;
            }
            ReflectionHelper.SetAttr(self, "mantis", false);
            ReflectionHelper.SetAttr(self, "longnail", false);
            ReflectionHelper.SetAttr(self, "fury", false);
            orig(self);
        }

        private void RedwingLance(PlayerData data, HeroController controller)
        {
            HeroController.instance.ATTACK_DURATION = 0.35f * 2f;
            HeroController.instance.ATTACK_DURATION_CH = 0.25f * 2f;
            HeroController.instance.ATTACK_COOLDOWN_TIME = 0.41f * 2f;
            HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 0.25f * 2f;
            HeroController.instance.BOUNCE_VELOCITY = 12f / 1.5f;
            HeroController.instance.BOUNCE_TIME = 0.25f * 2f;
            //Log("Bounce heights are " + HeroController.instance.BOUNCE_VELOCITY + " for time " + HeroController.instance.BOUNCE_TIME);
        }

        private void Update()
        {
            ktimer -= Time.unscaledDeltaTime;
            if (HeroController.instance.hero_state == ActorStates.airborne && _acceptingInput)
            {
                if (GameManager.instance.inputHandler.inputActions.up.State && KnightPhysics.velocity.y < _maxfallspeed)
                {
                    KnightPhysics.velocity = new Vector2(KnightPhysics.velocity.x, _maxfallspeed);
                    HeroController.instance.ResetHardLandingTimer();
                }
                if (_startedJetpack && GameManager.instance
                    .inputHandler.inputActions.dash.State && firePower > 0.3f * Time.deltaTime)
                {
                    firePower -= 0.3f * Time.deltaTime;
                    _jetpackCycle = true;
                    //Log("Doing jetpack");
                    //Jetpack mode
                    KnightPhysics.gravityScale = 0f;
                    HeroController.instance.cState.falling = false;
                    Vector2 velocity = KnightPhysics.velocity;
                    velocity = velocity.y + 20f * Time.deltaTime > 15f ? new Vector2(velocity.x, 15f) : new Vector2(velocity.x, velocity.y + 20f * Time.deltaTime);
                    KnightPhysics.velocity = velocity;
                } else if (_startedJetpack)
                {
                    KnightPhysics.gravityScale = HeroController.instance.DEFAULT_GRAVITY;
                    _startedJetpack = false;
                }
            } else if (_startedJetpack)
            {
                Log(HeroController.instance.hero_state + " is current hero state");
                KnightPhysics.gravityScale = HeroController.instance.DEFAULT_GRAVITY;
                _startedJetpack = false;
                _jetpackCycle = false;
            }
            
            if (firePower < 1.0 && HeroController.instance.hero_state == ActorStates.airborne && !_startedJetpack)
            {
                firePower += Time.deltaTime * 0.2;
                FireBarImage.fillAmount = (float) firePower;
            }
            else if (HeroController.instance.hero_state == ActorStates.airborne && !_startedJetpack)
            {
                firePower = 1.0;
                FireBarImage.fillAmount = 1.0f;
            } else if (firePower > 0.0)
            {
                firePower -= Time.deltaTime * 0.4;
                FireBarImage.fillAmount = (float) firePower;
            }
            else
            {
                firePower = 0;
                FireBarImage.fillAmount = 0f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.K) && ktimer < 0.0)
            {
                ktimer = 0.5;
                Log("K pressed. Outputting antistorage debug log");
                StorageDebugLog();
            } else if (UnityEngine.Input.GetKey(KeyCode.L) && ktimer < 0.0)
            {
                ktimer = 0.3;
                Log("L pressed. Hurting all enemies by 0.4 damage...");
                health.newhealthmanager.hurtAllEnemies(gameObject, 0.4);
            }
        }

        private void StorageDebugLog()
        {
            Log("First, jumpvars: ");
            Log("started jetpack? " + _startedJetpack + ", jetpack cycle? " + _jetpackCycle + "Gravity scale? " + KnightPhysics.gravityScale);
            Log("Herostate? " + HeroController.instance.hero_state + ", Is falling? " + HeroController.instance.cState.falling + ", Is jumping? " + HeroController.instance.cState.jumping);
            Log("Hitbox details: ");
            Log("Hitbox x, y is: " + KnightGameObject.GetComponent<BoxCollider2D>().size );
        }

        private IEnumerator ImmortalFreezeKnight(double time, bool resetVelocity)
        {
            HeroController.instance.SetDamageMode(1);
            _knightShouldBeInvuln = true;
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
            HeroController.instance.SetDamageMode(0);
            KnightPhysics.velocity = resetVelocity ? currentVelocity : Vector2.zero;
            _knightShouldBeInvuln = false;
            KnightPhysics.gravityScale = HeroController.instance.DEFAULT_GRAVITY;
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
            ReflectionHelper.SetAttr<CameraTarget, Transform>(GameManager.instance.cameraCtrl.camTarget,
                "heroTransform", GhostKnight.transform);
            while (startTime < time)
            {
                //CameraController.transform.position = (GhostKnight.transform.position - KnightGameObject.transform.position);
                //GameManager.instance.cameraCtrl.mode = global::CameraController.CameraMode.FROZEN;
                if (currentFrame < (int) (textures.WarpSprites.Length * (startTime / time)))
                {
                    currentFrame++;
                    GhostImage = textures.WarpSprites[currentFrame];
                }
                startTime += Time.deltaTime;
                yield return null;
            }

            HeroController.instance.acceptingInput = true;
            _acceptingInput = true;
            GhostSprite.color = Color.clear;
            GhostImage = textures.WarpSprites[0];
            GhostPhysics.velocity = Vector2.zero;
            KnightGameObject.transform.position = GhostKnight.transform.position;
            ReflectionHelper.SetAttr<CameraTarget, Transform>(GameManager.instance.cameraCtrl.camTarget,
                "heroTransform", KnightGameObject.transform);
            _jetpackCycle = false;
            _startedJetpack = false;
            KnightPhysics.gravityScale = HeroController.instance.DEFAULT_GRAVITY;
            HeroController.instance.cState.falling = true;
            HeroController.instance.ResetAirMoves();
        }
        

        private void InstanceOnAttackHook(AttackDirection dir)
        {
            _isUpSlashing = dir != AttackDirection.normal;
            if (dir == AttackDirection.upward && HeroController.instance.hero_state == ActorStates.airborne && firePower > 0.25 && !_knightShouldBeInvuln && _acceptingInput)
            {
                _acceptingInput = false;
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
                On.HeroController.Jump -= RocketJumpIfRocketJumping;
                Destroy(PillarDetectionObject);
                Destroy(FireBar);
                Destroy(canvasObj);
                Destroy(GhostKnight);
                Destroy(CeilingDetectionObject);
                Destroy(KnightLaserObject);
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
            //_baseGravityScale = KnightPhysics.gravityScale;
            //_baseGravityScale = 1f;

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
            
            // Basically replaces the "CheckCollisionSide" FSM action
            FloorDetectionObject = new GameObject("FloorDetect", typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(ceilingdetect), typeof(DebugColliders));
            FloorDetectionObject.transform.parent = KnightGameObject.transform;
            FloorDetectionObject.GetComponent<DebugColliders>().zDepth = 0f;
            FloorDetectionObject.layer = 0;
            // CheckCollisionSide detects from this distance
            FloorDetectionObject.transform.localPosition = new Vector3(0f, -0.7f, 0);
            _floordetect = FloorDetectionObject.GetComponent<ceilingdetect>();
            BoxCollider2D fCollider2D = FloorDetectionObject.GetComponent<BoxCollider2D>();
            fCollider2D.size = new Vector2(0.5f, 1.4f);
            Bounds fbounds = fCollider2D.bounds;
            fbounds.center = FloorDetectionObject.transform.position;
            fCollider2D.isTrigger = true;
            Rigidbody2D fFakePhysics = FloorDetectionObject.GetComponent<Rigidbody2D>();
            fFakePhysics.isKinematic = true;
            
            KnightLaserObject = new GameObject("LaserGenerator", typeof(laserControl), typeof(LineRenderer));
            KnightLaserObject.transform.position = KnightGameObject.transform.position;
            KnightLaserObject.transform.parent = KnightGameObject.transform;
            KnightLaser = KnightLaserObject.GetComponent<laserControl>();
            KnightLaser.lr = KnightLaserObject.GetComponent<LineRenderer>();
            
            GhostKnight.layer = 0;
            DontDestroyOnLoad(KnightLaserObject);
            DontDestroyOnLoad(GhostKnight);
            DontDestroyOnLoad(CeilingDetectionObject);
            DontDestroyOnLoad(FloorDetectionObject);
            Modding.Logger.LogDebug("Found Spell control and nail art control FSMs");
            //CameraController = GameObject.Find("CameraParent");
            Modding.ReflectionHelper.CacheFields<CameraTarget>();
            ReflectionHelper.CacheFields<HeroController>();
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