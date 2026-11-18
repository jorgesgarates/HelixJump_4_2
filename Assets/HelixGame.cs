using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class HelixGame : MonoBehaviour
{
    const int Sectors = 12;
    const float Spacing = 3f, Radius = 0.34f, Orbit = 2f;
    const string RecordKey = "HelixJump_HighScore";

    // Actividad 4.3: parametros editables desde el Inspector (antes de Play).
    [Header("1. Generacion procedural continua")]
    [SerializeField, Range(3, 20)] int plataformasAdelante = 8;
    [SerializeField, Range(0, 5)] int plataformasAtras = 2;
    [SerializeField, Min(1)] int plataformasPorNivel = 5;

    [Header("2. Curva de dificultad")]
    [SerializeField, Range(1, Sectors - 2)] int huecoInicial = 4;
    [SerializeField, Range(1, Sectors - 2)] int huecoMinimo = 1;
    [SerializeField, Range(0, 3)] int reduccionHuecoPorNivel = 1;
    [SerializeField, Min(0)] int plataformasSinPeligro = 3;
    [SerializeField, Range(0, Sectors - 2)] int peligrosIniciales = 1;
    [SerializeField, Range(0, Sectors - 2)] int peligrosMaximos = 4;
    [SerializeField, Range(0, 3)] int aumentoPeligroPorNivel = 1;
    [SerializeField, Min(1f)] float gravedadInicial = 14f;
    [SerializeField, Min(0f)] float aumentoGravedadPorNivel = 2f;
    [SerializeField, Min(1f)] float gravedadMaxima = 28f;
    [SerializeField, Min(1f)] float velocidadCaidaInicial = 12f;
    [SerializeField, Min(0f)] float aumentoVelocidadPorNivel = 2f;
    [SerializeField, Min(1f)] float velocidadCaidaMaxima = 24f;

    [Header("3. Game Loop y controles")]
    [SerializeField, Range(1f, 10f)] float segundosParaReiniciar = 3f;
    [SerializeField, Min(1)] int puntosPorPlataforma = 10;
    [SerializeField, Min(1f)] float velocidadGiro = 155f;
    [SerializeField, Min(0.01f)] float sensibilidadArrastre = 0.25f;
    [SerializeField, Min(0.5f)] float velocidadRebote = 6.3f;

    // Actividad 4.4: iluminacion, materiales y personalizacion en tiempo real.
    [Header("4. Fondo dinamico del entorno")]
    [SerializeField] bool cambiarFondoPorNivel = true;
    [SerializeField] Color[] coloresFondo = new Color[]
    {
        new Color(0.045f, 0.075f, 0.17f), // Azul nocturno
        new Color(0.27f, 0.07f, 0.34f),   // Violeta
        new Color(0.035f, 0.25f, 0.24f),  // Turquesa oscuro
        new Color(0.37f, 0.09f, 0.15f),   // Vino
        new Color(0.10f, 0.18f, 0.42f)    // Azul intenso
    };
    [SerializeField, Min(0.1f)] float velocidadTransicionColor = 4f;

    [Header("5. Personaje y materiales")]
    [SerializeField] Color[] coloresJugador = new Color[]
    {
        new Color(1f, 0.76f, 0.10f),      // Dorado
        new Color(0.95f, 0.22f, 0.82f),   // Magenta
        new Color(0.25f, 1f, 0.35f),      // Verde lima
        new Color(0.93f, 0.96f, 1f)       // Blanco
    };
    [SerializeField, Range(0f, 1f)] float metalicoJugador = 0.18f;
    [SerializeField, Range(0f, 1f)] float suavidadJugador = 0.82f;
    [SerializeField, Range(0f, 2f)] float emisionJugador = 0.40f;
    [SerializeField] Color colorSeguro = new Color(0.03f, 0.72f, 0.88f);
    [SerializeField] Color colorPeligro = new Color(1f, 0.16f, 0.25f);
    [SerializeField] Color colorColumna = new Color(0.32f, 0.40f, 0.56f);
    [SerializeField, Range(0f, 1f)] float suavidadPlataformas = 0.55f;
    [SerializeField, Range(0f, 1f)] float metalicoPlataformas = 0.12f;

    [Header("6. Iluminacion ambiental y direccional")]
    [SerializeField] Color colorAmbiente = new Color(0.45f, 0.49f, 0.60f);
    [SerializeField] Color colorLuz = new Color(1f, 0.94f, 0.84f);
    [SerializeField, Range(0f, 3f)] float intensidadLuz = 1.35f;
    [SerializeField] Vector3 rotacionLuz = new Vector3(48f, -35f, 0f);
    [SerializeField, Range(0f, 1f)] float intensidadSombras = 0.55f;
    [SerializeField] Color colorLuzRelleno = new Color(0.60f, 0.77f, 1f);
    [SerializeField, Range(0f, 2f)] float intensidadRelleno = 0.55f;
    [Header("7. Interfaz y navegacion - Actividad 4.5")]
    [SerializeField] Color colorPanel = new Color(0.035f, 0.065f, 0.13f, 0.98f);
    [SerializeField] Color colorBoton = new Color(0.06f, 0.48f, 0.65f);
    [SerializeField] bool reinicioAutomatico = true;
    const string EscenaMenu = "Menu";
    const string EscenaNivel1 = "Nivel1";
    const string EscenaNivel2 = "Nivel2";
    static int etapaGuardada = 1, puntosGuardados = 0;
    int etapaActual = 1, puntosInicioNivel;
    bool enMenu, enPausa, nivelCompletado, cargando;
    Transform uiRoot;
    GameObject hud, panel;
    Text panelDetalle;
    int temaManual, indiceJugador;
    Light luzPrincipal, luzRelleno;

    void OnValidate()
    {
        plataformasAdelante = Mathf.Clamp(plataformasAdelante, 3, 20);
        plataformasAtras = Mathf.Clamp(plataformasAtras, 0, 5);
        plataformasPorNivel = Mathf.Max(1, plataformasPorNivel);
        huecoInicial = Mathf.Clamp(huecoInicial, 1, Sectors - 2);
        huecoMinimo = Mathf.Clamp(huecoMinimo, 1, huecoInicial);
        reduccionHuecoPorNivel = Mathf.Clamp(reduccionHuecoPorNivel, 0, 3);
        plataformasSinPeligro = Mathf.Max(0, plataformasSinPeligro);
        peligrosIniciales = Mathf.Clamp(peligrosIniciales, 0, Sectors - 2);
        peligrosMaximos = Mathf.Clamp(peligrosMaximos, peligrosIniciales, Sectors - 2);
        aumentoPeligroPorNivel = Mathf.Clamp(aumentoPeligroPorNivel, 0, 3);
        gravedadInicial = Mathf.Max(1f, gravedadInicial);
        gravedadMaxima = Mathf.Max(gravedadInicial, gravedadMaxima);
        aumentoGravedadPorNivel = Mathf.Max(0f, aumentoGravedadPorNivel);
        velocidadCaidaInicial = Mathf.Max(1f, velocidadCaidaInicial);
        velocidadCaidaMaxima = Mathf.Max(velocidadCaidaInicial, velocidadCaidaMaxima);
        aumentoVelocidadPorNivel = Mathf.Max(0f, aumentoVelocidadPorNivel);
        segundosParaReiniciar = Mathf.Clamp(segundosParaReiniciar, 1f, 10f);
        puntosPorPlataforma = Mathf.Max(1, puntosPorPlataforma);
        velocidadGiro = Mathf.Max(1f, velocidadGiro);
        sensibilidadArrastre = Mathf.Max(0.01f, sensibilidadArrastre);
        velocidadRebote = Mathf.Max(0.5f, velocidadRebote);
        velocidadTransicionColor = Mathf.Max(0.1f, velocidadTransicionColor);
    }

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
    float tiempoReinicio;
    int Difficulty => etapaActual;
    int NivelDePlataforma(int index) => etapaActual;

    void Start()
    {
        OnValidate();
        string escena = SceneManager.GetActiveScene().name;
        enMenu = escena == EscenaMenu;
        if (escena != EscenaNivel2)
        {
            etapaGuardada = 1;
            puntosGuardados = 0;
        }
        else if (escena == EscenaNivel2)
            etapaGuardada = Mathf.Max(2, etapaGuardada);
        etapaActual = etapaGuardada;
        puntosInicioNivel = puntosGuardados;
        best = PlayerPrefs.GetInt(RecordKey, 0);
        safeMat = MakeMaterial(colorSeguro);
        dangerMat = MakeMaterial(colorPeligro);
        ballMat = MakeMaterial(ColorDePaleta(coloresJugador, 0, Color.yellow));
        pillarMat = MakeMaterial(colorColumna);
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
        cam.backgroundColor = ColorDePaleta(coloresFondo, 0, Color.black);
        cam.farClipPlane = 150f;
        ConfigurarIluminacion();
        CreateUI();
        ResetGame();
        if (enMenu) MostrarMenu();
    }

    Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    // Paletas configurables en Inspector; se admiten listas vacias.
    Color ColorDePaleta(Color[] paleta, int indice, Color respaldo)
    {
        if (paleta == null || paleta.Length == 0) return respaldo;
        return paleta[indice % paleta.Length];
    }

    void ConfigurarIluminacion()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = colorAmbiente;
        luzPrincipal = RenderSettings.sun;
        if (luzPrincipal == null || luzPrincipal.type != LightType.Directional)
        {
            luzPrincipal = null;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.isActiveAndEnabled)
                {
                    luzPrincipal = l;
                    break;
                }
        }
        if (luzPrincipal == null)
        {
            GameObject principal = new GameObject("Luz principal");
            principal.transform.SetParent(transform, false);
            luzPrincipal = principal.AddComponent<Light>();
        }
        luzPrincipal.type = LightType.Directional;
        luzPrincipal.shadows = LightShadows.Soft;
        RenderSettings.sun = luzPrincipal;
        GameObject relleno = new GameObject("Luz de relleno");
        relleno.transform.SetParent(transform, false);
        luzRelleno = relleno.AddComponent<Light>();
        luzRelleno.type = LightType.Directional;
        luzRelleno.transform.rotation = Quaternion.Euler(25f, 145f, 0f);
        luzRelleno.shadows = LightShadows.None;
        ActualizarLuces();
    }

    void ActualizarLuces()
    {
        RenderSettings.ambientLight = colorAmbiente;
        if (luzPrincipal != null)
        {
            luzPrincipal.color = colorLuz;
            luzPrincipal.intensity = intensidadLuz;
            luzPrincipal.transform.rotation = Quaternion.Euler(rotacionLuz);
            luzPrincipal.shadowStrength = intensidadSombras;
        }
        if (luzRelleno != null)
        {
            luzRelleno.color = colorLuzRelleno;
            luzRelleno.intensity = intensidadRelleno;
        }
    }

    void AplicarMaterial(Material mat, Color color, float metalico, float suavidad, float emision)
    {
        mat.color = color;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalico);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", suavidad);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", suavidad);
        if (mat.HasProperty("_EmissionColor"))
        {
            if (emision > 0f) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emision);
        }
    }

    void ActualizarVisuales(float mezcla)
    {
        int indiceFondo = temaManual + (cambiarFondoPorNivel ? Difficulty - 1 : 0);
        Color fondo = ColorDePaleta(coloresFondo, indiceFondo, Color.black);
        cam.backgroundColor = Color.Lerp(cam.backgroundColor, fondo, mezcla);
        Color colorJugador = ColorDePaleta(coloresJugador, indiceJugador, Color.yellow);
        AplicarMaterial(ballMat, colorJugador, metalicoJugador, suavidadJugador, emisionJugador);
        AplicarMaterial(safeMat, colorSeguro, metalicoPlataformas, suavidadPlataformas, 0f);
        AplicarMaterial(dangerMat, colorPeligro, 0.05f, 0.45f, 0.20f);
        AplicarMaterial(pillarMat, colorColumna, 0.10f, 0.45f, 0f);
        ActualizarLuces();
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
        nextRing = 0; lastRing = -1; score = puntosInicioNivel;
        angle = 0f; height = 2.2f; speed = 0f;
        cameraHeight = height; gameOver = false; tiempoReinicio = 0f;
        enPausa = false; nivelCompletado = false;
        CerrarPanel();
        if (hud != null) hud.SetActive(!enMenu);
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
        ActualizarVisuales(1f);
        UpdateUI();
    }

    void EnsureRings()
    {
        // Ventana movil: genera por delante y elimina modulos ya superados.
        // No existe un ultimo nivel predefinido.
        while (lastRing < nextRing + plataformasAdelante) CreateRing(++lastRing);
        List<int> old = new List<int>();
        foreach (var pair in rings)
            if (pair.Key < nextRing - plataformasAtras) old.Add(pair.Key);
        foreach (int i in old)
        {
            DeleteRing(rings[i]);
            rings.Remove(i);
        }
    }

    void CreateRing(int index)
    {
        Ring ring = new Ring();
        int nivel = NivelDePlataforma(index);
        ring.root = new GameObject("Nivel " + nivel + " - Plataforma " + (index + 1));
        ring.root.transform.SetParent(world.transform, false);
        ring.root.transform.localPosition = new Vector3(0, -index * Spacing, 0);
        // Cada nivel reduce la abertura hasta el minimo configurado.
        int tier = nivel - 1;
        int gapSize = Mathf.Max(huecoMinimo, huecoInicial - tier * reduccionHuecoPorNivel);
        int gapStart = index == 0 ? 3 : Random.Range(0, Sectors);
        for (int i = 0; i < Sectors; i++) ring.sectors[i] = 1;
        for (int j = 0; j < gapSize; j++) ring.sectors[(gapStart + j) % Sectors] = 0;
        List<int> safe = new List<int>();
        for (int i = 0; i < Sectors; i++) if (ring.sectors[i] == 1) safe.Add(i);
        // El peligro aumenta por nivel sin eliminar todos los sectores seguros.
        int hazards = index < plataformasSinPeligro ? 0 :
            Mathf.Min(peligrosMaximos, peligrosIniciales + tier * aumentoPeligroPorNivel);
        hazards = Mathf.Clamp(hazards, 0, safe.Count - 1);
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
        // V: cambia el tema del fondo. C: cambia el aspecto de la esfera.
        if (kb != null)
        {
            if (kb.vKey.wasPressedThisFrame && coloresFondo != null && coloresFondo.Length > 0)
                temaManual = (temaManual + 1) % coloresFondo.Length;
            if (kb.cKey.wasPressedThisFrame && coloresJugador != null && coloresJugador.Length > 0)
                indiceJugador = (indiceJugador + 1) % coloresJugador.Length;
        }
        ActualizarVisuales(1f - Mathf.Exp(-velocidadTransicionColor * Time.unscaledDeltaTime));
        if (cargando || enMenu || nivelCompletado) return;
        if (gameOver)
        {
            if (reinicioAutomatico) tiempoReinicio -= Time.unscaledDeltaTime;
            bool reinicioManual = kb != null &&
                (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);
            if ((reinicioAutomatico && tiempoReinicio <= 0f) || reinicioManual) ResetGame();
            else MostrarCuentaReinicio();
            return;
        }
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (enPausa) Reanudar(); else Pausar();
            return;
        }
        if (enPausa) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f), direction = 0f;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) direction -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) direction += 1f;
            if (kb.rKey.wasPressedThisFrame) { ResetGame(); return; }
        }
        angle += direction * velocidadGiro * dt;
        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
        {
            if (touch != null && touch.primaryTouch.press.isPressed)
                angle += touch.primaryTouch.delta.ReadValue().x * sensibilidadArrastre;
            else if (mouse != null && mouse.leftButton.isPressed)
                angle += mouse.delta.ReadValue().x * sensibilidadArrastre;
        }
        angle = Mathf.Repeat(angle, 360f);
        // Descenso continuo y rebote con deteccion de sectores por coordenadas.
        float gravedad = Mathf.Min(gravedadMaxima,
            gravedadInicial + (Difficulty - 1) * aumentoGravedadPorNivel);
        float limiteCaida = Mathf.Min(velocidadCaidaMaxima,
            velocidadCaidaInicial + (Difficulty - 1) * aumentoVelocidadPorNivel);
        speed -= gravedad * dt;
        speed = Mathf.Max(speed, -limiteCaida);
        float newHeight = height + speed * dt;
        while (speed < 0f)
        {
            float surface = -nextRing * Spacing + Radius;
            if (height < surface || newHeight > surface) break;
            int s = Mathf.FloorToInt(angle / (360f / Sectors)) % Sectors;
            int type = rings[nextRing].sectors[s];
            if (type == 1)
            {
                newHeight = surface; speed = velocidadRebote; break;
            }
            if (type == 2)
            {
                height = surface; SetBallPosition(); EndGame(); return;
            }
            // Una plataforma atravesada: sumar puntos y generar otra seccion.
            nextRing++;
            score += puntosPorPlataforma;
            if (score > best)
            {
                best = score;
                PlayerPrefs.SetInt(RecordKey, best);
                PlayerPrefs.Save();
            }
            if (nextRing >= plataformasPorNivel)
            {
                height = newHeight;
                SetBallPosition();
                UpdateCamera(1f);
                CompletarNivel();
                return;
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
        tiempoReinicio = segundosParaReiniciar;
        AbrirPanel("FIN DEL JUEGO", "", new string[] { "Reintentar", "Menu principal" },
            new System.Action[] { ResetGame, VolverMenu });
        MostrarCuentaReinicio();
        UpdateUI();
    }

    void MostrarCuentaReinicio()
    {
        if (panelDetalle == null) return;
        panelDetalle.text = "Puntos: " + score + "   |   Nivel: " + etapaActual +
            (reinicioAutomatico ? "\nReinicio en " + Mathf.Max(0, Mathf.CeilToInt(tiempoReinicio)) + " s" :
                "\nPulsa Reintentar para volver a jugar.");
    }

    // Actividad 4.5: navegacion entre escenas y estados de interfaz.
    void CargarEscena(string escena, int etapa, int puntos)
    {
        if (cargando) return;
        if (!Application.CanStreamedLevelBeLoaded(escena))
        {
            if (panelDetalle != null)
                panelDetalle.text = "No se encontro la escena " + escena + ".\nRevisa la lista de escenas de compilacion.";
            Debug.LogWarning("Escena no incluida en Build Profiles: " + escena);
            return;
        }
        etapaGuardada = etapa;
        puntosGuardados = puntos;
        tiempoReinicio = 0f;
        cargando = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena, LoadSceneMode.Single);
    }

    void Jugar() { CargarEscena(EscenaNivel1, 1, 0); }
    void VolverMenu() { CargarEscena(EscenaMenu, 1, 0); }
    void SiguienteNivel() { CargarEscena(EscenaNivel2, etapaActual + 1, score); }
    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void MostrarMenu()
    {
        enMenu = true;
        hud.SetActive(false);
        AbrirPanel("HELIX JUMP", "Desciende por la torre y supera cada nivel.\nRecord: " + best,
            new string[] { "Jugar", "Instrucciones", "Salir" },
            new System.Action[] { Jugar, MostrarInstrucciones, Salir });
    }

    void MostrarInstrucciones()
    {
        AbrirPanel("COMO JUGAR", "A / D o flechas: girar.\nArrastra con el raton o el dedo.\nEvita los sectores rojos y atraviesa los huecos.\nESC: pausa | V: fondo | C: color | R: reiniciar.",
            new string[] { "Volver" }, new System.Action[] { MostrarMenu });
    }

    void Pausar()
    {
        if (enMenu || gameOver || nivelCompletado || cargando) return;
        enPausa = true;
        AbrirPanel("PAUSA", "Nivel " + etapaActual + "   |   Puntos: " + score,
            new string[] { "Continuar", "Reiniciar nivel", "Menu principal" },
            new System.Action[] { Reanudar, ResetGame, VolverMenu });
    }

    void Reanudar()
    {
        enPausa = false;
        CerrarPanel();
    }

    void CompletarNivel()
    {
        nivelCompletado = true;
        UpdateUI();
        AbrirPanel("NIVEL " + etapaActual + " COMPLETADO", "Plataformas superadas: " + nextRing +
            " / " + plataformasPorNivel + "\nPuntuacion acumulada: " + score,
            new string[] { "Siguiente nivel", "Menu principal" },
            new System.Action[] { SiguienteNivel, VolverMenu });
    }

    void CerrarPanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            Destroy(panel);
        }
        panel = null;
        panelDetalle = null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void CreateUI()
    {
        GameObject obj = new GameObject("Interfaz Canvas", typeof(RectTransform));
        obj.transform.SetParent(transform, false);
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1f;
        obj.AddComponent<GraphicRaycaster>();
        uiRoot = obj.transform;
        EventSystem eventos = EventSystem.current;
        if (eventos == null)
        {
            GameObject sistema = new GameObject("EventSystem");
            sistema.transform.SetParent(transform, false);
            eventos = sistema.AddComponent<EventSystem>();
        }
        if (eventos.GetComponent<InputSystemUIInputModule>() == null)
        {
            foreach (BaseInputModule anterior in eventos.GetComponents<BaseInputModule>())
                anterior.enabled = false;
            InputSystemUIInputModule modulo = eventos.gameObject.AddComponent<InputSystemUIInputModule>();
            if (modulo.actionsAsset == null) modulo.AssignDefaultActions();
        }
        hud = new GameObject("HUD", typeof(RectTransform));
        hud.transform.SetParent(uiRoot, false);
        Estirar(hud.GetComponent<RectTransform>());
        scoreText = MakeText(hud.transform, "Puntuacion", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(22, -14), new Vector2(300, 112), 27, TextAnchor.UpperLeft);
        bestText = MakeText(hud.transform, "Record", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-22, -14), new Vector2(300, 50), 26, TextAnchor.UpperRight);
        MakeButton(hud.transform, "Pausa", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-22, -74), new Vector2(140, 48), Pausar);
        MakeButton(hud.transform, "Fondo", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(18, 18), new Vector2(130, 46), CambiarFondo);
        MakeButton(hud.transform, "Esfera", new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-18, 18), new Vector2(130, 46), CambiarEsfera);
        Text help = MakeText(hud.transform, "Controles", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 18), new Vector2(350, 48), 21, TextAnchor.MiddleCenter);
        help.text = "A / D o arrastrar para girar";
        messageText = MakeText(hud.transform, "Mensaje", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(400, 80), 25, TextAnchor.MiddleCenter);
    }

    void CambiarFondo()
    {
        if (coloresFondo != null && coloresFondo.Length > 0)
            temaManual = (temaManual + 1) % coloresFondo.Length;
    }
    void CambiarEsfera()
    {
        if (coloresJugador != null && coloresJugador.Length > 0)
            indiceJugador = (indiceJugador + 1) % coloresJugador.Length;
    }

    void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    void AbrirPanel(string titulo, string detalle, string[] nombres, System.Action[] acciones)
    {
        CerrarPanel();
        panel = new GameObject("Panel " + titulo, typeof(RectTransform));
        panel.transform.SetParent(uiRoot, false);
        Estirar(panel.GetComponent<RectTransform>());
        Image sombra = panel.AddComponent<Image>();
        sombra.color = new Color(0.015f, 0.025f, 0.06f, 0.74f);
        GameObject tarjeta = new GameObject("Tarjeta", typeof(RectTransform));
        tarjeta.transform.SetParent(panel.transform, false);
        RectTransform rt = tarjeta.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(560, 540);
        tarjeta.AddComponent<Image>().color = colorPanel;
        // El panel se adapta tambien a pantallas verticales.
        float anchoDisponible = 720f * Screen.width / Mathf.Max(1f, Screen.height);
        float escala = Mathf.Min(1f, Mathf.Max(0.1f, (anchoDisponible - 24f) / 560f));
        rt.localScale = Vector3.one * escala;
        Text encabezado = MakeText(tarjeta.transform, "Titulo", new Vector2(.5f, 1), new Vector2(.5f, 1),
            new Vector2(0, -25), new Vector2(520, 85), 38, TextAnchor.MiddleCenter);
        encabezado.text = titulo;
        encabezado.color = new Color(.55f, .95f, 1f);
        panelDetalle = MakeText(tarjeta.transform, "Detalle", new Vector2(.5f, 1), new Vector2(.5f, 1),
            new Vector2(0, -112), new Vector2(510, 168), 23, TextAnchor.MiddleCenter);
        panelDetalle.text = detalle;
        for (int i = 0; i < nombres.Length; i++)
            MakeButton(tarjeta.transform, nombres[i], new Vector2(.5f, 1), new Vector2(.5f, 1),
                new Vector2(0, -290 - i * 70), new Vector2(410, 58), acciones[i]);
    }

    Button MakeButton(Transform parent, string nombre, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, System.Action accion)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        Image fondo = obj.AddComponent<Image>();
        fondo.color = colorBoton;
        Button boton = obj.AddComponent<Button>();
        boton.targetGraphic = fondo;
        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(.83f, 1f, 1f);
        colores.pressedColor = new Color(.65f, .78f, .85f);
        boton.colors = colores;
        Navigation nav = boton.navigation; nav.mode = Navigation.Mode.None; boton.navigation = nav;
        Text texto = MakeText(obj.transform, "Texto", new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            Vector2.zero, size, 24, TextAnchor.MiddleCenter);
        texto.text = nombre;
        boton.onClick.AddListener(() => accion());
        return boton;
    }

    void OnDestroy()
    {
        foreach (Ring ring in rings.Values)
            if (ring.root != null)
                foreach (MeshFilter f in ring.root.GetComponentsInChildren<MeshFilter>(true))
                    if (f.sharedMesh != null && f.sharedMesh.name == "HelixSector") Destroy(f.sharedMesh);
        if (safeMat != null) Destroy(safeMat);
        if (dangerMat != null) Destroy(dangerMat);
        if (ballMat != null) Destroy(ballMat);
        if (pillarMat != null) Destroy(pillarMat);
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
        scoreText.text = "PUNTOS: " + score + "\nNIVEL: " + etapaActual +
            "\nPlataformas: " + nextRing + " / " + plataformasPorNivel;
        bestText.text = "RECORD: " + best;
    }
}
