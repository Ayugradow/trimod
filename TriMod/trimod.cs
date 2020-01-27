using System.Collections;
using UnityEngine;
using Modding;
using Reflection = Modding.ReflectionHelper;
using UnityEngine.SceneManagement;


namespace TriMod
{
	public class TriMod : Mod
	{

		public override void Initialize()
		{
			LogDebug("TriMod initializing!");

			ModHooks.Instance.NewGameHook += SetupGameRefs;
			ModHooks.Instance.SavegameLoadHook += SetupGameRefs;
//			UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;

			LogDebug("TriMod v." + GetVersion() + " initialized!");
		}

		private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			if (GameManager.instance.IsGameplayScene() && UIManager.instance.uiState.ToString() == "PLAYING")
			{
				if (hc == null && HeroController.instance != null)
				{
					hc = HeroController.instance;
					if (spriteFlash == null)
					{
						spriteFlash = hc.GetComponent<SpriteFlash>();
						LogDebug("Hero object set. SpriteFlash component gotten.");
					}
				}
			}

			if (pd == null && PlayerData.instance != null)
				pd = PlayerData.instance;
		}

        

		public void SetupGameRefs()
		{
			if (gm == null)
			{
				gm = GameManager.instance;
			}
			
			if (pd == null && PlayerData.instance != null)
				pd = PlayerData.instance;
			
			if (GameManager.instance.IsGameplayScene() && UIManager.instance.uiState.ToString() == "PLAYING")
			{
				if (hc == null && HeroController.instance != null)
				{
					hc = HeroController.instance;
					if (spriteFlash == null)
					{
						spriteFlash = hc.GetComponent<SpriteFlash>();
						LogDebug("Hero object set. SpriteFlash component gotten.");
					}
				}
			}

			// Uncomment for void.
			gm.gameObject.AddComponent<Blackmoth.Berserker>();
			
			// Uncomment for Redwing.
//			Redwing.Knight rk = gm.gameObject.AddComponent<Redwing.Knight>();
//			rk.EnableRedwing();
		}

		public void SetupGameRefs(int id)
		{
			SetupGameRefs();
		}
        
		public static GameManager gm;
		public static PlayerData pd;
		public static HeroController hc;
		public static SpriteFlash spriteFlash;
		public int voidCount = 0;
		public bool isBerserk = false;
	}
}