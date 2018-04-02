using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TimeLapse
{
    public class TimeLapseManager : MonoBehaviour
    {
        public class CameraPoint
        {
            public Vector3 pos;

            public Quaternion rot;

            public float CamSize;
            public void DrawGUI()
            {
            }
        }

        private Camera camera;

        private int resHeight;

        private int resWidth;

        private bool takeHiResShot;

        private bool TimeLapsing;

        private float SecBetween;

        private GUISkin _skin;
        private float CurSecBetween;
        private float lastTime;
        private int CurScreenShotCount;

        private int ScreenShotCount;

        private bool followPath;
        private string Name = "New TimeLapse";
        private CameraPoint Preview;

        private List<CameraPoint> CamPoints = new List<CameraPoint>();

        private List<CameraPoint> SmoothCamPoints = new List<CameraPoint>();
        bool previewing;
        int CurPreview = -1;
        bool PlayCurve;
        public string Path;
        public GameObject GOPreview;
        public Rect windowRect = new Rect(20f, 20f, 230f, 500f);

        private void Start()
        {
            resHeight = Screen.height;
            resWidth = Screen.width;

            //StartCoroutine(LoadGUISkin());
            camera = Camera.main;
        }
        private IEnumerator LoadGUISkin()
        {
            char dsc = System.IO.Path.DirectorySeparatorChar;

            using (WWW www = new WWW("file://" + Path + dsc + "assetbundle" + dsc + "guiskin"))
            {
                yield return www;

                _skin = www.assetBundle.LoadAsset<GUISkin>("ParkitectGUISkin");

                www.assetBundle.Unload(false);
            }
        }
        private void Update()
        {
            if (TimeLapsing)
            {
                CurSecBetween += Time.realtimeSinceStartup - lastTime;
                lastTime = Time.realtimeSinceStartup;
                if (CurSecBetween >= SecBetween)
                {
                    TakeHiResShot();
                    CurScreenShotCount++;
                    if (CurScreenShotCount > ScreenShotCount && followPath)
                    {
                        TimeLapsing = false;
                        CurScreenShotCount = 0;
                    }
                    CurSecBetween = 0f;
                }
            }
            if (PlayCurve)
            {
                CurSecBetween += Time.deltaTime;
                if (CurSecBetween >= SecBetween)
                {
                    CurSecBetween = 0;
                    PlayCurve = false;
                }
                CurPreview = (int)(CurSecBetween / SecBetween * SmoothCamPoints.Count);
            }
        }

        private void SetCamOnPoint(CameraPoint CP)
        {
            camera.transform.position = CP.pos;
            camera.transform.rotation = CP.rot;
        }

        private void AddCamPoint(Vector3 pos, Quaternion rot)
        {
            CameraPoint cameraPoint = new CameraPoint();
            cameraPoint.pos = pos;
            cameraPoint.rot = rot;
            CamPoints.Add(cameraPoint);
            Debug.Log("Camera Point Added");
        }

        private void SetSmoothCamPoints()
        {
            SmoothCamPoints = new List<CameraPoint>();
            int num = ScreenShotCount / CamPoints.Count();
            Vector3[] array = Curver.MakeSmoothCurve((from C in CamPoints
                select C.pos).ToArray(), num);
            Quaternion[] array2 = Curver.MakeSmoothRost((from C in CamPoints
                select C.rot).ToArray(), num);
            for (int i = 0; i < array.Count(); i++)
            {
                CameraPoint cameraPoint = new CameraPoint();
                cameraPoint.pos = array[i];
                cameraPoint.rot = array2[i];
                SmoothCamPoints.Add(cameraPoint);
            }
        }

        public static string ScreenShotName(int width, int height, string Path, int Num, string Name)
        {
            return string.Format("{0}/screenshots/{4}_screen_{1}x{2}_{3}.png", Path, width, height, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "-" + Num, Name);
        }

        public void TakeHiResShot()
        {
            HidePreview();
            takeHiResShot = true;
        }
        public void HidePreview()
        {

            previewing = false;
            try { Destroy(GOPreview); }
            catch { }
            try { gameObject.GetComponent<LineRenderer>().SetVertexCount(0); }
            catch { }

        }
        void ShowPreview()
        {
            previewing = true;
            GOPreview = new GameObject();
            foreach (CameraPoint CP in CamPoints)
            {
                GameObject NewSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                NewSphere.transform.position = CP.pos;
                NewSphere.transform.rotation = CP.rot;
                NewSphere.transform.localScale = NewSphere.transform.localScale / 2;
                NewSphere.transform.SetParent(GOPreview.transform);
            }
            //Smooth Curve
            SetSmoothCamPoints();
            LineRenderer LR;
            if (gameObject.GetComponent<LineRenderer>())
                LR = gameObject.GetComponent<LineRenderer>();
            else
            {
                LR = GOPreview.AddComponent<LineRenderer>();
                LR.SetWidth(.2f, .2f);
                LR.useWorldSpace = true;
                LR.material = AssetManager.Instance.dataViewMaterial;
            }
            LR.SetVertexCount(SmoothCamPoints.Count);
            for (int i = 0; i < SmoothCamPoints.Count; i++)
            {
                LR.SetPosition(i, SmoothCamPoints[i].pos);
            }
        }
        private void LateUpdate()
        {

            if (CurPreview != -1)
            {
                SetCamOnPoint(SmoothCamPoints[CurPreview]);
            }
            if (Preview != null)
            {
                SetCamOnPoint(Preview);

            }
            if (takeHiResShot)
            {
                CameraPoint StartCP = new CameraPoint();
                StartCP.pos = camera.transform.position;
                StartCP.rot = camera.transform.rotation;
                camera.enabled = false;
                float LoadBias = QualitySettings.lodBias;
                QualitySettings.lodBias = Mathf.Infinity;
                ScriptableSingleton<UIAssetManager>.Instance.uiWindowFrameGO.enabled = false;
                if (followPath)
                {
                    SetCamOnPoint(SmoothCamPoints[CurScreenShotCount]);
                }
                else
                {
                    if (CamPoints.Count > 0)

                        SetCamOnPoint(CamPoints[0]);
                }
                float StartNear = camera.nearClipPlane;
                float StartFar = camera.farClipPlane;
                camera.nearClipPlane = 0f;
                camera.farClipPlane = 1600f;
                RenderTexture renderTexture = new RenderTexture(resWidth, resHeight, 24);
                camera.targetTexture = renderTexture;
                Texture2D texture2D = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = renderTexture;
                texture2D.ReadPixels(new Rect(0f, 0f, resWidth, resHeight), 0, 0);
                SetCamOnPoint(StartCP);
                camera.enabled = true;
                camera.targetTexture = null;
                RenderTexture.active = null;
                Destroy(renderTexture);
                byte[] bytes = texture2D.EncodeToPNG();
                string text = ScreenShotName(resWidth, resHeight, Path, CurScreenShotCount, Name);
                File.WriteAllBytes(text, bytes);
                Debug.Log(string.Format("Took screenshot to: {0}", text));
                Destroy(texture2D);
                QualitySettings.lodBias = LoadBias;
                camera.nearClipPlane = StartNear;
                camera.farClipPlane = StartFar;
                takeHiResShot = false;
            }
        }

        private void OnGUI()
        {
            //GUI.skin = _skin;
            windowRect = GUI.Window(902, windowRect, DoMyWindow, "TimeLapse Setup");
        }

        private void DoMyWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("TimeLapse Name");
            Name = GUILayout.TextField(Name);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Res Height: ");
            try
            {
                resHeight = int.Parse(GUILayout.TextField(resHeight.ToString()));
            }
            catch
            {
            }
            if (GUILayout.Button("-")) { resHeight = (int)(resHeight / 2f); }
            if (GUILayout.Button("+")) { resHeight = (int)(resHeight * 2f); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Res Width: ");
            try
            {
                resWidth = int.Parse(GUILayout.TextField(resWidth.ToString()));
            }
            catch
            {
            }

            if (GUILayout.Button("-")) { resWidth = (int)(resWidth / 2f); }
            if (GUILayout.Button("+")) { resWidth = (int)(resWidth * 2f); }
            GUILayout.EndHorizontal();
            if (followPath)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("ScreenShotCount: ");
                try
                {
                    ScreenShotCount = int.Parse(GUILayout.TextField(ScreenShotCount.ToString()));
                }
                catch
                {
                }
       
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("SecBetween: ");
            try
            {
                SecBetween = float.Parse(GUILayout.TextField(SecBetween.ToString()));
            }
            catch
            {
            }
            GUILayout.EndHorizontal();
            followPath = GUILayout.Toggle(followPath, " Follow Path");
            GUILayout.BeginVertical("box");
            int number = 1;
            foreach (var CP in CamPoints)
            {
                if (Preview == CP)
                {
                    GUI.color = Color.green;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Point " + number);
                number++;
                if (GUILayout.Button("Delete"))
                {
                    if (Preview == CP)
                    { Preview = null; }
                    CamPoints.Remove(CP);
                }


                if (GUILayout.Button("view"))
                {
                    if (Preview != CP) { Preview = CP; }
                    else { Preview = null; }

                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Add Camera Point"))
            {
                if (!followPath)
                {
                    CamPoints = new List<CameraPoint>();
                    Preview = null;
                }
                AddCamPoint(camera.transform.position, camera.transform.rotation);
            }
            string Text = "";
            if (previewing)
                Text = "Stop Preview";
            else
                Text = "Preview TimeLapse points";
            if (GUILayout.Button(Text))
            {
                if (!previewing)
                    ShowPreview();
                else
                    HidePreview();

            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start TimeLapse"))
            {
                SetSmoothCamPoints();
                lastTime = Time.realtimeSinceStartup;
                TimeLapsing = true;
            }
            if (GUILayout.Button("Stop TimeLapse"))
            {
                TimeLapsing = false;
                CurScreenShotCount = 0;
                CurSecBetween = 0;
            }
            GUILayout.EndHorizontal();

            if (TimeLapsing)
            {
                GUILayout.Label("TimeLapsing... Frame: " + CurScreenShotCount);
            }
            if (followPath)
            {
                if (PlayCurve)
                    Text = "Stop Playing";
                else
                    Text = "Play along Curve";
                if (GUILayout.Button(Text))
                {
                    CurPreview = -1;
                    CurSecBetween = 0;
                    PlayCurve = !PlayCurve;
                }
                CurPreview = (int)GUILayout.HorizontalSlider(CurPreview, 0, SmoothCamPoints.Count - 1);
                if (GUILayout.Button("Reset"))
                {
                    CurPreview = -1;
                }
            }
            if (GUILayout.Button("TakeScreenshot"))
            {
                TakeHiResShot();
            }
            GUILayout.FlexibleSpace();

        }
    }
}
