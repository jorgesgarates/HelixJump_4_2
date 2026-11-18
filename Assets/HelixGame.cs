using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HelixGame : MonoBehaviour
{
    const int Sectors = 12;
    const float Spacing = 3f, Radius = 0.34f, Orbit = 2f;
    const string RecordKey = "HelixJump_HighScore";

    class Ring
    {
        public GameObject root;
        public int[] sectors = new int[Sectors]; // 0: hueco; 1: seguro; 2: peligro
    }
    readonly Dictionary<int, Ring> rings = new Dictionary<int, Ring>();
    GameObject world;
    Transform ball;
    Camera cam;
    Material safeMat, dangerMat, ballMat, pillarMat;
    Text scoreText, bestText, messageText;
    int nextRing, lastRing, score, best;
    float angle, height, speed, cameraHeight;
    bool gameOver;
    int Difficulty => 1 + score / 50;

    void Start()
    {
        best = PlayerPrefs.GetInt(RecordKey, 0);
        safeMat = MakeMaterial(new Color(0.05f, 0.73f, 0.84f));
        dangerMat = MakeMaterial(new Color(1f, 0.18f, 0.30f));
        ballMat = MakeMaterial(new Color(1f, 0.77f, 0.12f));
        pillarMat = MakeMaterial(new Color(0.26f, 0.32f, 0.46f));
        cam = Camera.main;
        if (cam == null)
        {
            GameObject c = new GameObject("Main Camera");
            c.tag = "MainCamera";
            cam = c.AddComponent<Camera>();
        }
        cam.orthographic = false;
        cam.fieldOfView = 52f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.08f, 0.15f);
        cam.farClipPlane = 150f;
        CreateUI();
        ResetGame();
    }

    Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    void DeleteRing(Ring ring)
    {
        foreach (MeshFilter f in ring.root.GetComponentsInChildren<MeshFilter>(true))
            if (f.sharedMesh != null && f.sharedMesh.name == "HelixSector")
                Destroy(f.sharedMesh);
        Destroy(ring.root);
    }

    void ResetGame()
    {
        if (world != null)
        {
            world.SetActive(false);
            foreach (Ring ring in rings.Values) DeleteRing(ring);
            Destroy(world);
        }
        rings.Clear();
        world = new GameObject("Torre Helix");
        world.transform.SetParent(transform, false);
        nextRing = 0; lastRing = -1; score = 0;
        angle = 0f; height = 2.2f; speed = 0f;
        cameraHeight = height; gameOver = false;
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Jugador";
        player.transform.SetParent(world.transform, false);
        player.transform.localScale = Vector3.one * Radius * 2f;
        player.GetComponent<Renderer>().sharedMaterial = ballMat;
        Destroy(player.GetComponent<Collider>());
        ball = player.transform;
        EnsureRings();
        SetBallPosition();
        UpdateCamera(1f);
        messageText.text = "";
        UpdateUI();
    }

    void EnsureRings()
    {
        while (lastRing < nextRing + 8) CreateRing(++lastRing);
        List<int> old = new List<int>();
        foreach (var pair in rings)
            if (pair.Key < nextRing - 2) old.Add(pair.Key);
        foreach (int i in old)
        {
            DeleteRing(rings[i]);
            rings.Remove(i);
        }
    }

    void CreateRing(int index)
    {
        Ring ring = new Ring();
        ring.root = new GameObject("Plataforma " + (index + 1));
        ring.root.transform.SetParent(world.transform, false);
        ring.root.transform.localPosition = new Vector3(0, -index * Spacing, 0);
        // Cada cinco plataformas disminuye el hueco y aumenta el peligro.
        int tier = index / 5;
        int gapSize = Mathf.Max(1, 4 - tier);
        int gapStart = index == 0 ? 3 : Random.Range(0, Sectors);
        for (int i = 0; i < Sectors; i++) ring.sectors[i] = 1;
        for (int j = 0; j < gapSize; j++) ring.sectors[(gapStart + j) % Sectors] = 0;
        List<int> safe = new List<int>();
        for (int i = 0; i < Sectors; i++) if (ring.sectors[i] == 1) safe.Add(i);
        int hazards = index < 3 ? 0 : Mathf.Min(4, 1 + tier);
        for (int j = 0; j < hazards && safe.Count > 1; j++)
        {
            int k = Random.Range(0, safe.Count);
            ring.sectors[safe[k]] = 2;
            safe.RemoveAt(k);
        }
        for (int i = 0; i < Sectors; i++)
            if (ring.sectors[i] != 0)
                CreateSector(ring.root.transform, i, ring.sectors[i] == 2 ? dangerMat : safeMat);
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Columna";
        pillar.transform.SetParent(ring.root.transform, false);
        pillar.transform.localPosition = new Vector3(0, -Spacing / 2, 0);
        pillar.transform.localScale = new Vector3(0.85f, Spacing / 2, 0.85f);
        pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
        Destroy(pillar.GetComponent<Collider>());
        rings.Add(index, ring);
    }

    Vector3 Point(float r, float degrees, float y)
    {
        float a = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * r, y, -Mathf.Cos(a) * r);
    }

    void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int n = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(n); t.Add(n + 1); t.Add(n + 2);
        t.Add(n); t.Add(n + 2); t.Add(n + 3);
    }

    void CreateSector(Transform parent, int sector, Material material)
    {
        const float inner = 0.43f, outer = 2.85f, bottom = -0.25f;
        const int steps = 6;
        float width = 360f / Sectors, start = sector * width;
        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        for (int j = 0; j < steps; j++)
        {
            float a = start + width * j / steps, b = start + width * (j + 1) / steps;
            Vector3 ia = Point(inner, a, 0), ib = Point(inner, b, 0);
            Vector3 oa = Point(outer, a, 0), ob = Point(outer, b, 0);
            Vector3 iad = Point(inner, a, bottom), ibd = Point(inner, b, bottom);
            Vector3 oad = Point(outer, a, bottom), obd = Point(outer, b, bottom);
            Quad(v, t, ia, ib, ob, oa);
            Quad(v, t, iad, oad, obd, ibd);
            Quad(v, t, oa, ob, obd, oad);
            Quad(v, t, ia, iad, ibd, ib);
            if (j == 0) Quad(v, t, ia, oa, oad, iad);
            if (j == steps - 1) Quad(v, t, ib, ibd, obd, ob);
        }
        Mesh mesh = new Mesh();
        mesh.name = "HelixSector";
        mesh.SetVertices(v); mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GameObject segment = new GameObject("Sector " + sector);
        segment.transform.SetParent(parent, false);
        segment.AddComponent<MeshFilter>().sharedMesh = mesh;
        segment.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        Touchscreen touch = Touchscreen.current;
        if (gameOver)
        {
            if ((kb != null && (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                (touch != null && touch.primaryTouch.press.wasPressedThisFrame)) ResetGame();
            return;
        }
        float dt = Mathf.Min(Time.deltaTime, 0.05f), direction = 0f;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) direction -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) direction += 1f;
            if (kb.rKey.wasPressedThisFrame) { ResetGame(); return; }
        }
        angle += direction * 155f * dt;
        if (touch != null && touch.primaryTouch.press.isPressed)
            angle += touch.primaryTouch.delta.ReadValue().x * 0.25f;
        else if (mouse != null && mouse.leftButton.isPressed)
            angle += mouse.delta.ReadValue().x * 0.25f;
        angle = Mathf.Repeat(angle, 360f);
        // Descenso continuo y rebote con deteccion de sectores por coordenadas.
        speed -= (14f + Mathf.Min(14f, (Difficulty - 1) * 2f)) * dt;
        speed = Mathf.Max(speed, -(12f + Mathf.Min(12f, (Difficulty - 1) * 2f)));
        float newHeight = height + speed * dt;
        while (speed < 0f)
        {
            float surface = -nextRing * Spacing + Radius;
            if (height < surface || newHeight > surface) break;
            int s = Mathf.FloorToInt(angle / (360f / Sectors)) % Sectors;
            int type = rings[nextRing].sectors[s];
            if (type == 1)
            {
                newHeight = surface; speed = 6.3f; break;
            }
            if (type == 2)
            {
                height = surface; SetBallPosition(); EndGame(); return;
            }
            // Incremento uniforme: diez puntos por plataforma atravesada.
            nextRing++; score += 10;
            if (score > best)
            {
                best = score;
                PlayerPrefs.SetInt(RecordKey, best);
                PlayerPrefs.Save();
            }
            UpdateUI(); EnsureRings();
        }
        height = newHeight;
        SetBallPosition();
        UpdateCamera(1f - Mathf.Exp(-6f * dt));
    }

    void SetBallPosition() { ball.position = Point(Orbit, angle, height); }

    void UpdateCamera(float blend)
    {
        cameraHeight = Mathf.Lerp(cameraHeight, height, blend);
        cam.transform.position = Point(9.2f, angle, cameraHeight + 5f);
        cam.transform.LookAt(new Vector3(0, cameraHeight - 1.4f, 0));
    }

    void EndGame()
    {
        gameOver = true;
        PlayerPrefs.SetInt(RecordKey, best);
        PlayerPrefs.Save();
        messageText.text = "FIN DEL JUEGO\nPuntos: " + score + "\nR / Espacio / Clic para reiniciar";
        UpdateUI();
    }

    void CreateUI()
    {
        GameObject obj = new GameObject("Interfaz", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        scoreText = MakeText(obj.transform, "Puntuacion", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(25, -20), new Vector2(450, 105), 35, TextAnchor.UpperLeft);
        bestText = MakeText(obj.transform, "Record", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-25, -20), new Vector2(450, 90), 30, TextAnchor.UpperRight);
        Text help = MakeText(obj.transform, "Controles", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 22), new Vector2(1150, 60), 24, TextAnchor.MiddleCenter);
        help.text = "A / D o flechas | Arrastra con raton o dedo | R: reiniciar";
        messageText = MakeText(obj.transform, "Mensaje", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1000, 250), 44, TextAnchor.MiddleCenter);
        messageText.color = new Color(1f, 0.85f, 0.3f);
    }

    Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize; text.fontStyle = FontStyle.Bold;
        text.color = Color.white; text.alignment = alignment; text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.85f);
        shadow.effectDistance = new Vector2(2, -2);
        return text;
    }

    void UpdateUI()
    {
        scoreText.text = "PUNTOS: " + score + "\nDificultad: " + Difficulty;
        bestText.text = "RECORD: " + best;
    }
}
