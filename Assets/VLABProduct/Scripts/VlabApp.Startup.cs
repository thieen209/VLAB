using System.Collections;
using UnityEngine;
using TMPro;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        const string TeamCredit="Made by Ngựa Mán team with love";
        IEnumerator ShowStartup()
        {
            var brand=NewPage("boot");
            var foreground=brand.gameObject.AddComponent<Canvas>();foreground.overrideSorting=true;foreground.sortingOrder=ui.canvas.sortingOrder+1;
            var background=ui.Rect(ui.canvas.transform,"Startup background",0,0,1600,1000);
            background.anchorMin=Vector2.zero;background.anchorMax=Vector2.one;background.offsetMin=background.offsetMax=Vector2.zero;
            var fill=background.gameObject.AddComponent<UnityEngine.UI.Image>();fill.color=VlabUi.Hex("07121C");fill.raycastTarget=false;
            background.SetSiblingIndex(brand.GetSiblingIndex());
            Logo(brand,540,350,520,165);
            ui.Text(brand,TeamCredit,300,570,1000,70,28,VlabUi.Secondary,TextAlignmentOptions.Center);
            var fade=brand.gameObject.AddComponent<CanvasGroup>();fade.blocksRaycasts=false;fade.interactable=false;
            var backgroundFade=background.gameObject.AddComponent<CanvasGroup>();backgroundFade.blocksRaycasts=false;
            float visibleAt=Time.realtimeSinceStartup;
            yield return null;
            AdoptScene();ApplySettings();
            // Keep the credit readable even when cached room preparation completes within one frame.
            while(Time.realtimeSinceStartup-visibleAt<1.1f)yield return null;
            page=null;ShowSetupOrHome();background.SetAsLastSibling();brand.SetAsLastSibling();
            if(!Profile.settings.reducedMotion)
            {
                for(float elapsed=0;elapsed<.24f&&brand;elapsed+=Time.unscaledDeltaTime)
                {fade.alpha=backgroundFade.alpha=1-Mathf.Clamp01(elapsed/.24f);yield return null;}
            }
            if(brand)Destroy(brand.gameObject);
            if(background)Destroy(background.gameObject);
        }
    }
}
