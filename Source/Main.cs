using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ApparelX
{
	public class ApparelXMain : Mod
	{
		public static bool onlyDeadMansApparel;

		public ApparelXMain(ModContentPack content) : base(content)
		{
			var harmony = HarmonyInstance.Create("net.pardeike.apparelx");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		public static bool Filter(Tradeable item)
		{
			if (ApparelXMain.onlyDeadMansApparel == false)
				return true;

			var apparel = item.AnyThing as Apparel;
			if (apparel == null) return false;
			return apparel.WornByCorpse;
		}
	}

	[HarmonyPatch(typeof(Dialog_Trade))]
	[HarmonyPatch("FillMainRect")]
	static class Dialog_Trade_FillMainRect_Patch
	{
		static void Prefix(Dialog_Trade __instance, out List<Tradeable> __state)
		{
			var f_cachedTradeables = Traverse.Create(__instance).Field("cachedTradeables");
			__state = f_cachedTradeables.GetValue<List<Tradeable>>();
			f_cachedTradeables.SetValue(__state.Where(item => ApparelXMain.Filter(item)).ToList());
		}

		static void Postfix(Dialog_Trade __instance, List<Tradeable> __state)
		{
			Traverse.Create(__instance).Field("cachedTradeables").SetValue(__state);
		}
	}

	[HarmonyPatch(typeof(Dialog_Trade))]
	[HarmonyPatch("DoWindowContents")]
	static class Dialog_Trade_DoWindowContents_Patch
	{
		static void Postfix(Dialog_Trade __instance, Rect inRect)
		{
			Rect rect;

			var acceptButtonSize = Traverse.Create(__instance).Field("AcceptButtonSize").GetValue<Vector2>();
			var otherBottomButtonSize = Traverse.Create(__instance).Field("OtherBottomButtonSize").GetValue<Vector2>();

			var voff = (otherBottomButtonSize.y - 24f) / 2;
			rect = new Rect(inRect.width / 2f + 1.5f * acceptButtonSize.x + 25f, inRect.height - 55f + voff, otherBottomButtonSize.x, 24f);
			Widgets.CheckboxLabeled(rect, "Only Deadmans Cloth", ref ApparelXMain.onlyDeadMansApparel);

			rect = new Rect(inRect.width / 2f - 2.5f * acceptButtonSize.x - 20f, inRect.height - 55f, otherBottomButtonSize.x, otherBottomButtonSize.y);
			if (Widgets.ButtonText(rect, "Sell All", true, false, true))
			{
				var f_cachedTradeables = Traverse.Create(__instance).Field("cachedTradeables");
				f_cachedTradeables.GetValue<List<Tradeable>>().Do(item =>
				{
					var amountToSell = item.GetMinimumToTransfer();
					if (ApparelXMain.Filter(item) && amountToSell < 0)
						item.AdjustTo(amountToSell);
				});
			}
		}
	}
}