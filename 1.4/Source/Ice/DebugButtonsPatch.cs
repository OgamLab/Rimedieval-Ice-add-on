using HarmonyLib;
using UnityEngine;
using Verse;

namespace Ice
{
	[HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
	[StaticConstructorOnStartup]
	internal static class DebugButtonsPatch
	{
		[HarmonyPatch(typeof(WidgetRow), "ButtonIcon")]
		private static class WidgetRowGetter
		{
			private static void Prefix(WidgetRow __instance)
			{
				if (run)
				{
					run = false;
					row = __instance;
				}
			}
		}

		private static Texture2D butt = ContentFinder<Texture2D>.Get("UI/icesaw");

		private static WidgetRow row;

		private static bool run;

		public static bool DrawDebugOverlay { get; private set; }

		private static void Prefix()
		{
			run = true;
		}

		private static void Postfix()
		{
			run = false;
			if (Prefs.DevMode && Current.ProgramState == ProgramState.Playing && row != null)
			{
				Draw();
			}
			row = null;
		}

		private static void Draw()
		{
			if (row.ButtonIcon(butt, "Ice Debug"))
			{
				DrawDebugOverlay = !DrawDebugOverlay;
			}
		}
	}
}
