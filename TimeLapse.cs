using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TimeLapse : MonoBehaviour
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

    private List<TimeLapse.CameraPoint> CamPoints = new List<TimeLapse.CameraPoint>();

    private List<TimeLapse.CameraPoint> SmoothCamPoints = new List<TimeLapse.CameraPoint>();
    bool previewing;
    int CurPreview = -1;
    bool PlayCurve;
    public string Path;
    public GameObject GOPreview;
    public Rect windowRect = new Rect(20f, 20f, 230f, 500f);

    private void Start()
    {
        this.resHeight = Screen.height;
        this.resWidth = Screen.width;

        StartCoroutine(LoadGUISkin());
        this.camera = Camera.main;
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
        if (this.TimeLapsing)
        {
            this.CurSecBetween += Time.realtimeSinceStartup - lastTime;
            lastTime = Time.realtimeSinceStartup;
            if (this.CurSecBetween >= this.SecBetween)
            {
                this.TakeHiResShot();
                this.CurScreenShotCount++;
                if (this.CurScreenShotCount > this.ScreenShotCount && this.followPath)
                {
                    this.TimeLapsing = false;
                    this.CurScreenShotCount = 0;
                }
                this.CurSecBetween = 0f;
            }
        }
        if (PlayCurve)
        {
            this.CurSecBetween += Time.deltaTime;
            if (this.CurSecBetween >= this.SecBetween)
            {
                CurSecBetween = 0;
                PlayCurve = false;
            }
            CurPreview = (int)(CurSecBetween / SecBetween * SmoothCamPoints.Count);
        }
    }

    private void SetCamOnPoint(TimeLapse.CameraPoint CP)
    {
        this.camera.transform.position = CP.pos;
        this.camera.transform.rotation = CP.rot;
    }

    private void AddCamPoint(Vector3 pos, Quaternion rot)
    {
        TimeLapse.CameraPoint cameraPoint = new TimeLapse.CameraPoint();
        cameraPoint.pos = pos;
        cameraPoint.rot = rot;
        this.CamPoints.Add(cameraPoint);
        Debug.Log("Camera Point Added");
    }

    private void SetSmoothCamPoints()
    {
        SmoothCamPoints = new List<CameraPoint>();
        int num = this.ScreenShotCount / this.CamPoints.Count<TimeLapse.CameraPoint>();
        Vector3[] array = Curver.MakeSmoothCurve((from C in this.CamPoints
                                                  select C.pos).ToArray<Vector3>(), (float)num);
        Quaternion[] array2 = Curver.MakeSmoothRost((from C in this.CamPoints
                                                     select C.rot).ToArray<Quaternion>(), (float)num);
        for (int i = 0; i < array.Count<Vector3>(); i++)
        {
            TimeLapse.CameraPoint cameraPoint = new TimeLapse.CameraPoint();
            cameraPoint.pos = array[i];
            cameraPoint.rot = array2[i];
            this.SmoothCamPoints.Add(cameraPoint);
        }
    }

    public static string ScreenShotName(int width, int height, string Path, int Num, string Name)
    {
        return string.Format("{0}/screenshots/{4}_screen_{1}x{2}_{3}.png", new object[]
        {
            Path,
            width,
            height,
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "-" + Num ,
            Name
        });
    }

    public void TakeHiResShot()
    {
        HidePreview();
        this.takeHiResShot = true;
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
        if (this.takeHiResShot)
        {
            CameraPoint StartCP = new CameraPoint();
            StartCP.pos = camera.transform.position;
            StartCP.rot = camera.transform.rotation;
            camera.enabled = false;
            float LoadBias = QualitySettings.lodBias;
            QualitySettings.lodBias = Mathf.Infinity;
            ScriptableSingleton<UIAssetManager>.Instance.uiWindowFrameGO.enabled = false;
            if (this.followPath)
            {
                this.SetCamOnPoint(this.SmoothCamPoints[this.CurScreenShotCount]);
            }
            else
            {
                if (CamPoints.Count > 0)

                    this.SetCamOnPoint(this.CamPoints[0]);
            }
            float StartNear = camera.nearClipPlane;
            float StartFar = camera.farClipPlane;
            camera.nearClipPlane = 0f;
            camera.farClipPlane = 1600f;
            RenderTexture renderTexture = new RenderTexture(this.resWidth, this.resHeight, 24);
            this.camera.targetTexture = renderTexture;
            Texture2D texture2D = new Texture2D(this.resWidth, this.resHeight, TextureFormat.RGB24, false);
            this.camera.Render();
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0f, 0f, (float)this.resWidth, (float)this.resHeight), 0, 0);
            SetCamOnPoint(StartCP);
            camera.enabled = true;
            this.camera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
            byte[] bytes = texture2D.EncodeToPNG();
            string text = TimeLapse.ScreenShotName(this.resWidth, this.resHeight, this.Path, CurScreenShotCount, Name);
            File.WriteAllBytes(text, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", text));
            Destroy(texture2D);
            QualitySettings.lodBias = LoadBias;
            camera.nearClipPlane = StartNear;
            camera.farClipPlane = StartFar;
            this.takeHiResShot = false;
        }
    }

    private void OnGUI()
    {
        GUI.skin = _skin;
        this.windowRect = GUI.Window(902, this.windowRect, new GUI.WindowFunction(this.DoMyWindow), "TimeLapse Setup");
    }

    private void DoMyWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("TimeLapse Name");
        Name = GUILayout.TextField(Name);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Label("Res Height: ", new GUILayoutOption[0]);
        try
        {
            this.resHeight = int.Parse(GUILayout.TextField(this.resHeight.ToString(), new GUILayoutOption[0]));
        }
        catch
        {
        }
        if (GUILayout.Button("-")) { resHeight = (int)(resHeight / 2f); }
        if (GUILayout.Button("+")) { resHeight = (int)(resHeight * 2f); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Label("Res Width: ", new GUILayoutOption[0]);
        try
        {
            this.resWidth = int.Parse(GUILayout.TextField(this.resWidth.ToString(), new GUILayoutOption[0]));
        }
        catch
        {
        }

        if (GUILayout.Button("-")) { resWidth = (int)(resWidth / 2f); }
        if (GUILayout.Button("+")) { resWidth = (int)(resWidth * 2f); }
        GUILayout.EndHorizontal();
        if (followPath)
        {
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("ScreenShotCount: ", new GUILayoutOption[0]);
            try
            {
                this.ScreenShotCount = int.Parse(GUILayout.TextField(this.ScreenShotCount.ToString(), new GUILayoutOption[0]));
            }
            catch
            {
            }
       
        GUILayout.EndHorizontal();
        }
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        GUILayout.Label("SecBetween: ", new GUILayoutOption[0]);
        try
        {
            this.SecBetween = float.Parse(GUILayout.TextField(this.SecBetween.ToString(), new GUILayoutOption[0]));
        }
        catch
        {
        }
        GUILayout.EndHorizontal();
        this.followPath = GUILayout.Toggle(this.followPath, " Follow Path", new GUILayoutOption[0]);
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
        if (GUILayout.Button("Add Camera Point", new GUILayoutOption[0]))
        {
            if (!followPath)
            {
                this.CamPoints = new List<TimeLapse.CameraPoint>();
                Preview = null;
            }
            this.AddCamPoint(this.camera.transform.position, this.camera.transform.rotation);
        }
        string Text = "";
        if (previewing)
            Text = "Stop Preview";
        else
            Text = "Preview TimeLapse points";
        if (GUILayout.Button(Text, new GUILayoutOption[0]))
        {
            if (!previewing)
                ShowPreview();
            else
                HidePreview();

        }
        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
        if (GUILayout.Button("Start TimeLapse", new GUILayoutOption[0]))
        {
            SetSmoothCamPoints();
            lastTime = Time.realtimeSinceStartup;
            this.TimeLapsing = true;
        }
        if (GUILayout.Button("Stop TimeLapse", new GUILayoutOption[0]))
        {
            this.TimeLapsing = false;
            CurScreenShotCount = 0;
            CurSecBetween = 0;
        }
        GUILayout.EndHorizontal();

        if (this.TimeLapsing)
        {
            GUILayout.Label("TimeLapsing... Frame: " + this.CurScreenShotCount, new GUILayoutOption[0]);
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
