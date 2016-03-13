using UnityEngine;

namespace HelloMod
{
    public class Main : IMod
    {
        public string Identifier { get; set; }
        GameObject _go;
        TimeLapse DC;
        public void onEnabled()
        {
            DC = Camera.main.gameObject.AddComponent<TimeLapse>();
            DC.Path = Path;
            Debug.Log("TimeLapse Mod Enabled");
        }

        public void onDisabled()
        {
            DC.HidePreview();
            UnityEngine.Object.DestroyImmediate(DC);
        }

        public string Name 
        {
            get { return "TimeLapse"; }
        }

        public string Path { get; set; }
        public string Description
        {
            get { return "Create a Timelapse while building your park"; }
        }
    }
}
