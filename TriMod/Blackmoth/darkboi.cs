using System.Collections;
using GlobalEnums;
using UnityEngine;
using Reflection = Modding.ReflectionHelper;
using Modding;
using Logger = Modding.Logger;

namespace TriMod.Blackmoth
{
	public class Berserker : MonoBehaviour
	{
		public bool isBerserk = false;
		public int voidCount = 0;
		AttackTypes Claw = (AttackTypes) 8;

		private static GUIStyle labelStyle;
		private static GUIStyle buttonStyle;
		public Font trajanBold;
		public Font trajanNormal;

		private void Start()
		{
			On.HeroController.LookForInput += HeroControllerOnLookForInput;
			On.HealthManager.Hit += HealthManagerOnHit;
			On.NailSlash.StartSlash += NailSlashOnStartSlash;
			On.HeroController.NailParry += HeroControllerOnNailParry;
			ModHooks.Instance.CharmUpdateHook += ClawSpeed;
		}

		private void HeroControllerOnNailParry(On.HeroController.orig_NailParry orig, HeroController self)
		{
			if (voidCount < 5)
				voidCount++;
			Logger.Log("Added 10 mana");

			orig(self);
		}
		
		private void HealthManagerOnHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitinstance)
		{
			if (isBerserk)
			{
				hitinstance.DamageDealt = 10;
			}
			else
			{
				return;
			}

			orig(self, hitinstance);
		}

		private void HeroControllerOnLookForInput(On.HeroController.orig_LookForInput orig, HeroController self)
		{
			if (Reflection.GetAttr<HeroController, InputHandler>(HeroController.instance, "inputHandler")
				    .inputActions.dash.WasPressed
			    && voidCount > 4)
			{
				isBerserk = true;
				StartCoroutine(BerserkMode());
			}

			orig(self);
		}

		IEnumerator BerserkMode()
		{
			while (isBerserk && voidCount > 0)
			{
				yield return new WaitForSeconds(2);
				voidCount --;
			}

			isBerserk = !isBerserk;
			voidCount = 0;
		}

		private void NailSlashOnStartSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
		{
			self.scale.x = 0.7f;
			self.scale.y = 0.7f;
			Reflection.SetAttr(self, "mantis", false);
			Reflection.SetAttr(self, "longnail", false);
			Reflection.SetAttr(self, "fury", false);
			orig(self);
		}

		public void ClawSpeed(PlayerData pd, HeroController hc)
		{
			HeroController.instance.ATTACK_DURATION = 0.35f / 10;
			HeroController.instance.ATTACK_DURATION_CH = 0.25f / 10;
			HeroController.instance.ATTACK_COOLDOWN_TIME = 0.41f / 10;
			HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 0.25f / 10;
		}

		public void OnGUI()
		{
			if (GameManager.instance == null) return;
			if (GameManager.instance.gameState != GameState.PLAYING) return;

			if (trajanBold == null || trajanNormal == null)
			{
				foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
				{
					if (font != null && font.name == "TrajanPro-Bold")
					{
						trajanBold = font;
					}

					if (font != null && font.name == "TrajanPro-Regular")
					{
						trajanNormal = font;
					}
				}
			}


			GUI.enabled = true;
			if (labelStyle == null)
				labelStyle = new GUIStyle(GUI.skin.label)
				{
					font = trajanNormal,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleCenter,
					fontSize = 15
				};
			if (buttonStyle == null)
				buttonStyle = new GUIStyle(GUI.skin.button)
				{
					font = trajanBold,
					fontStyle = FontStyle.Normal,
					fontSize = 15,
					alignment = TextAnchor.MiddleCenter
				};
			GUI.backgroundColor = Color.white;
			GUI.contentColor = Color.white;
			GUI.color = Color.white;


			GUILayout.BeginArea(new Rect(20f, (float) (Screen.height / 4), 530f, 500f));
			GUILayout.Label("Voids " + voidCount, labelStyle);
		}
	}
}