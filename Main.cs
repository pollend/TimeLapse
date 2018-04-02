using System.Linq;
using UnityEngine;

namespace TimeLapse
{
    public class Main : IMod
    {
        public string Identifier { get; set; }
        GameObject _go;
        TimeLapseManager DC;
        public void onEnabled()
        {
            DC = Camera.main.gameObject.AddComponent<TimeLapseManager>();
            DC.Path = Path;
            Debug.Log("TimeLapse Mod Enabled");
        }

        public void onDisabled()
        {
            DC.HidePreview();
            Object.DestroyImmediate(DC);
        }

        public string Name => "Timelapse";
        public string Description => "Create a Timelapse while building your park";
        string IMod.Identifier => "Timelapse";
        public string Path
        {
            get { return ModManager.Instance.getModEntries().First(x => x.mod == this).path; }
        }
    }
}
