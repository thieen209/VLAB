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
            ui.Panel(brand,"Startup background",0,0,1600,1000).GetComponent<UnityEngine.UI.Image>().color=VlabUi.Hex("07121C");
            Logo(brand,540,350,520,165);
            ui.Text(brand,TeamCredit,300,570,1000,70,28,VlabUi.Secondary,TextAlignmentOptions.Center);
            var fade=brand.gameObject.AddComponent<CanvasGroup>();fade.blocksRaycasts=false;fade.interactable=false;
            // Render the brand before synchronous preparation; Home never waits for the decorative fade.
            yield return null;
            AdoptScene();ApplySettings();
            page=null;ShowSetupOrHome();brand.SetAsLastSibling();
            if(!Profile.settings.reducedMotion)
            {
                for(float elapsed=0;elapsed<.24f&&brand;elapsed+=Time.unscaledDeltaTime)
                {fade.alpha=1-Mathf.Clamp01(elapsed/.24f);yield return null;}
            }
            if(brand)Destroy(brand.gameObject);
        }
    }
}
