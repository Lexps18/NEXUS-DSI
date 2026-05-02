using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class NexusController : MonoBehaviour
{
    // Datos
    private List<HeroData> _heroes = new();
    private HeroData _selected;
    private int _currentLevel = 1;
    private string _pickedImagePath = "";

    // Root
    private VisualElement root;

    // Tabs y paneles
    private Button tabHeroes, tabNew, tabStats;
    private VisualElement panelHeroes, panelNew, panelStats;

    // Panel heroes
    private TextField searchField;
    private VisualElement heroGrid;
    private VisualElement detailPanel;
    private Label detailInitials, detailName, detailSub;
    private VisualElement barVida, barAtk, barDef;
    private Label valVida, valAtk, valDef;
    private Button btnEdit, btnDelete;

    // Formulario
    private TextField inputName;
    private DropdownField inputClass;
    private Label levelDisplay;
    private Button btnLevelUp, btnLevelDown;
    private SliderInt inputVida, inputAtk, inputDef;
    private Button btnPickImage, btnExamineImage, btnAdd;
    private Label imageNameLabel;
    private VisualElement imagePreview;

    // Stats
    private Label valTotal, valAvgLevel, valBest, valBestSub, valPower;
    private VisualElement tableBody;

    // Feedback
    private Label feedbackMsg;
    private Button btnSave;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        root.RegisterCallback<GeometryChangedEvent>(OnLayoutReady);
    }

    void OnLayoutReady(GeometryChangedEvent evt)
    {
        root.UnregisterCallback<GeometryChangedEvent>(OnLayoutReady);
        BindAll();
        LoadFromJson();
        SwitchTab(0);
        RefreshHeroGrid();
    }

    void BindAll()
    {
        // Tabs
        tabHeroes = root.Q<Button>("tab-heroes");
        tabNew = root.Q<Button>("tab-new");
        tabStats = root.Q<Button>("tab-stats");
        panelHeroes = root.Q<VisualElement>("panel-heroes");
        panelNew = root.Q<VisualElement>("panel-new");
        panelStats = root.Q<VisualElement>("panel-stats");

        tabHeroes.clicked += () => SwitchTab(0);
        tabNew.clicked += () => SwitchTab(1);
        tabStats.clicked += () => SwitchTab(2);

        // Guardar
        btnSave = root.Q<Button>("btn-save");
        btnSave.clicked += SaveToJson;

        // Busqueda
        searchField = root.Q<TextField>("search-field");
        searchField.RegisterValueChangedCallback(e => RefreshHeroGrid());

        // Hero grid y detail
        heroGrid = root.Q<VisualElement>("hero-grid");
        detailInitials = root.Q<Label>("detail-initials");
        detailName = root.Q<Label>("detail-name");
        detailSub = root.Q<Label>("detail-sub");
        barVida = root.Q<VisualElement>("bar-vida");
        barAtk = root.Q<VisualElement>("bar-atk");
        barDef = root.Q<VisualElement>("bar-def");
        valVida = root.Q<Label>("val-vida");
        valAtk = root.Q<Label>("val-atk");
        valDef = root.Q<Label>("val-def");
        btnEdit = root.Q<Button>("btn-edit");
        btnDelete = root.Q<Button>("btn-delete");

        btnEdit.clicked += OnEditClicked;
        btnDelete.clicked += OnDeleteClicked;

        // Formulario
        inputName = root.Q<TextField>("input-name");
        inputClass = root.Q<DropdownField>("input-class");
        levelDisplay = root.Q<Label>("level-display");
        btnLevelUp = root.Q<Button>("btn-level-up");
        btnLevelDown = root.Q<Button>("btn-level-down");
        inputVida = root.Q<SliderInt>("input-vida");
        inputAtk = root.Q<SliderInt>("input-atk");
        inputDef = root.Q<SliderInt>("input-def");
        btnPickImage = root.Q<Button>("btn-pick-image");
        btnExamineImage = root.Q<Button>("btn-examine-image");
        imageNameLabel = root.Q<Label>("image-name-label");
        imagePreview = root.Q<VisualElement>("image-preview");
        btnAdd = root.Q<Button>("btn-add");

        btnLevelUp.clicked += () => ChangeLevel(1);
        btnLevelDown.clicked += () => ChangeLevel(-1);
        btnPickImage.clicked += PickImage;
        btnAdd.clicked += OnAddHero;

        // Stats
        valTotal = root.Q<Label>("val-total");
        valAvgLevel = root.Q<Label>("val-avg-level");
        valBest = root.Q<Label>("val-best");
        valBestSub = root.Q<Label>("val-best-sub");
        valPower = root.Q<Label>("val-power");
        tableBody = root.Q<VisualElement>("table-body");

        // Feedback
        feedbackMsg = root.Q<Label>("feedback-msg");
    }

    // ── TABS ──────────────────────────────────────────
    void SwitchTab(int index)
    {
        panelHeroes.RemoveFromClassList("panel--active");
        panelNew.RemoveFromClassList("panel--active");
        panelStats.RemoveFromClassList("panel--active");
        tabHeroes.RemoveFromClassList("tab--active");
        tabNew.RemoveFromClassList("tab--active");
        tabStats.RemoveFromClassList("tab--active");

        switch (index)
        {
            case 0:
                panelHeroes.AddToClassList("panel--active");
                tabHeroes.AddToClassList("tab--active");
                break;
            case 1:
                panelNew.AddToClassList("panel--active");
                tabNew.AddToClassList("tab--active");
                ResetForm();
                break;
            case 2:
                panelStats.AddToClassList("panel--active");
                tabStats.AddToClassList("tab--active");
                RefreshStats();
                break;
        }
    }

    // ── HERO GRID ─────────────────────────────────────
    void RefreshHeroGrid()
    {
        heroGrid.Clear();
        string filter = searchField?.value?.ToLower() ?? "";

        foreach (var h in _heroes)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !h.nombre.ToLower().Contains(filter) &&
                !h.clase.ToLower().Contains(filter))
                continue;

            var card = CreateCard(h);
            heroGrid.Add(card);
        }
    }

    VisualElement CreateCard(HeroData h)
    {
        var uxml = Resources.Load<VisualTreeAsset>("HeroCard");
        var card = uxml.CloneTree();

        card.Q<Label>("card-name").text = h.nombre;
        card.Q<Label>("card-sub").text = $"{h.clase} · Nivel {h.nivel}";
        card.Q<Label>("card-initials").text = h.Iniciales();
        card.Q<Label>("card-vida").text = $"V {h.vida}";
        card.Q<Label>("card-atk").text = $"A {h.ataque}";
        card.Q<Label>("card-def").text = $"D {h.defensa}";

        var badge = card.Q<Label>("card-badge");
        badge.text = h.clase.ToUpper();
        badge.ClearClassList();
        badge.AddToClassList("card-badge");
        badge.AddToClassList(h.BadgeClass());

        // Imagen
        if (!string.IsNullOrEmpty(h.imagenPath) && File.Exists(h.imagenPath))
        {
            var tex = LoadTexture(h.imagenPath);
            if (tex != null)
            {
                var img = card.Q<VisualElement>("card-image");
                img.style.backgroundImage = new StyleBackground(tex);
                img.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
                card.Q<Label>("card-initials").style.display = DisplayStyle.None;
            }
        }

        // Observer: click selecciona heroe
        var cardRoot = card.Q<VisualElement>("hero-card");
        cardRoot.RegisterCallback<ClickEvent>(e => SelectHero(h));

        return card;
    }

    // ── OBSERVER: SELECCIÓN ───────────────────────────
    void SelectHero(HeroData h)
    {
        _selected = h;

        // Quitar seleccion de todas las cards
        foreach (var c in heroGrid.Children())
        {
            var cr = c.Q<VisualElement>("hero-card");
            cr?.RemoveFromClassList("hero-card--selected");
        }

        // Marcar la seleccionada
        foreach (var c in heroGrid.Children())
        {
            var cr = c.Q<VisualElement>("hero-card");
            var nameLabel = cr?.Q<Label>("card-name");
            if (nameLabel != null && nameLabel.text == h.nombre)
                cr.AddToClassList("hero-card--selected");
        }

        // Actualizar panel detalle
        detailInitials.text = h.Iniciales();
        detailName.text = h.nombre;
        detailSub.text = $"{h.clase}  ·  Nivel {h.nivel}";

        float pctV = h.vida / 100f;
        float pctA = h.ataque / 100f;
        float pctD = h.defensa / 100f;

        barVida.style.width = new StyleLength(new Length(pctV * 100f, LengthUnit.Percent));
        barVida.style.backgroundColor = new Color(0.79f, 0.30f, 0.30f);
        barAtk.style.width = new StyleLength(new Length(pctA * 100f, LengthUnit.Percent));
        barAtk.style.backgroundColor = new Color(0.79f, 0.66f, 0.30f);
        barDef.style.width = new StyleLength(new Length(pctD * 100f, LengthUnit.Percent));
        barDef.style.backgroundColor = new Color(0.30f, 0.48f, 0.79f);

        valVida.text = h.vida.ToString();
        valAtk.text = h.ataque.ToString();
        valDef.text = h.defensa.ToString();

        // Imagen en detalle
        if (!string.IsNullOrEmpty(h.imagenPath) && File.Exists(h.imagenPath))
        {
            var tex = LoadTexture(h.imagenPath);
            if (tex != null)
            {
                var avatar = root.Q<VisualElement>("detail-avatar");
                avatar.style.backgroundImage = new StyleBackground(tex);
                avatar.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
                detailInitials.style.display = DisplayStyle.None;
            }
        }
        else
        {
            var avatar = root.Q<VisualElement>("detail-avatar");
            avatar.style.backgroundImage = StyleKeyword.Null;
            detailInitials.style.display = DisplayStyle.Flex;
        }
    }

    // ── EDITAR / ELIMINAR ────────────────────────────
    void OnEditClicked()
    {
        if (_selected == null) return;
        // Rellena el formulario con los datos del heroe seleccionado
        inputName.value = _selected.nombre;
        int claseIdx = inputClass.choices.IndexOf(_selected.clase);
        inputClass.index = claseIdx >= 0 ? claseIdx : 0;
        _currentLevel = _selected.nivel;
        levelDisplay.text = _currentLevel.ToString();
        inputVida.value = _selected.vida;
        inputAtk.value = _selected.ataque;
        inputDef.value = _selected.defensa;
        _pickedImagePath = _selected.imagenPath ?? "";
        imageNameLabel.text = string.IsNullOrEmpty(_pickedImagePath)
            ? "Ninguna seleccionada"
            : Path.GetFileName(_pickedImagePath);
        // Elimina el heroe para que al añadir lo reemplace
        _heroes.Remove(_selected);
        _selected = null;
        RefreshHeroGrid();
        SwitchTab(1);
    }

    void OnDeleteClicked()
    {
        if (_selected == null) return;
        _heroes.Remove(_selected);
        _selected = null;
        detailName.text = "Selecciona un heroe";
        detailSub.text = "";
        detailInitials.text = "??";
        barVida.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
        barAtk.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
        barDef.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
        valVida.text = "0"; valAtk.text = "0"; valDef.text = "0";
        RefreshHeroGrid();
    }

    // ── FORMULARIO ───────────────────────────────────
    void ResetForm()
    {
        if (inputName == null) return;
        inputName.value = "";
        inputClass.index = 0;
        _currentLevel = 1;
        levelDisplay.text = "1";
        inputVida.value = 50;
        inputAtk.value = 50;
        inputDef.value = 50;
        _pickedImagePath = "";
        if (imageNameLabel != null)
            imageNameLabel.text = "Ninguna seleccionada";
        if (imagePreview != null)
            imagePreview.style.backgroundImage = StyleKeyword.Null;
    }

    void ChangeLevel(int delta)
    {
        _currentLevel = Mathf.Clamp(_currentLevel + delta, 1, 20);
        levelDisplay.text = _currentLevel.ToString();
    }

    void PickImage()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Seleccionar imagen", "", "png");
        if (!string.IsNullOrEmpty(path))
        {
            _pickedImagePath = path;
            imageNameLabel.text = Path.GetFileName(path);
            var tex = LoadTexture(path);
            if (tex != null && imagePreview != null)
            {
                imagePreview.style.backgroundImage = new StyleBackground(tex);
                imagePreview.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
                var placeholder = imagePreview.Q<Label>();
                if (placeholder != null)
                    placeholder.style.display = DisplayStyle.None;
            }
        }
#endif
    }

    void OnAddHero()
    {
        if (string.IsNullOrEmpty(inputName.value)) return;

        var h = new HeroData
        {
            id = System.Guid.NewGuid().ToString(),
            nombre = inputName.value,
            clase = inputClass.choices[inputClass.index],
            nivel = _currentLevel,
            vida = inputVida.value,
            ataque = inputAtk.value,
            defensa = inputDef.value,
            imagenPath = _pickedImagePath
        };

        _heroes.Add(h);
        RefreshHeroGrid();
        SwitchTab(0);
        SelectHero(h);
    }

    // ── STATS ────────────────────────────────────────
    void RefreshStats()
    {
        valTotal.text = _heroes.Count.ToString();
        if (_heroes.Count == 0)
        {
            valAvgLevel.text = "0";
            valBest.text = "-";
            valBestSub.text = "";
            valPower.text = "0";
            tableBody.Clear();
            return;
        }

        float avg = 0;
        int totalPow = 0;
        HeroData best = _heroes[0];

        foreach (var h in _heroes)
        {
            avg += h.nivel;
            totalPow += h.Poder;
            if (h.Poder > best.Poder) best = h;
        }

        valAvgLevel.text = (avg / _heroes.Count).ToString("F1");
        valBest.text = best.nombre;
        valBestSub.text = $"{best.clase} · Nivel {best.nivel}";
        valPower.text = totalPow.ToString();

        // Tabla ordenada por poder
        tableBody.Clear();
        var sorted = new List<HeroData>(_heroes);
        sorted.Sort((a, b) => b.Poder.CompareTo(a.Poder));

        foreach (var h in sorted)
        {
            var row = new VisualElement();
            row.AddToClassList("table-row");

            var nombre = new Label(h.nombre);
            nombre.style.flexGrow = 2;
            nombre.style.fontSize = 13;
            nombre.style.color = new Color(0.91f, 0.91f, 0.94f);

            var badge = new Label(h.clase.ToUpper());
            badge.AddToClassList("badge");
            badge.AddToClassList(h.BadgeClass());
            badge.style.flexGrow = 1;

            var nivel = new Label(h.nivel.ToString());
            nivel.style.flexGrow = 1;
            nivel.style.fontSize = 13;
            nivel.style.color = new Color(0.91f, 0.91f, 0.94f);

            var poder = new Label(h.Poder.ToString());
            poder.style.flexGrow = 1;
            poder.style.fontSize = 13;
            poder.style.color = new Color(0.79f, 0.66f, 0.30f);
            poder.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(nombre); row.Add(badge);
            row.Add(nivel); row.Add(poder);
            tableBody.Add(row);
        }
    }

    // ── JSON ─────────────────────────────────────────
    void SaveToJson()
    {
        var col = new HeroCollection { heroes = _heroes };
        string json = JsonUtility.ToJson(col, true);
        string path = Path.Combine(Application.persistentDataPath, "nexus_heroes.json");
        File.WriteAllText(path, json);
        ShowFeedback();
    }

    void LoadFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "nexus_heroes.json");
        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        var col = JsonUtility.FromJson<HeroCollection>(json);
        if (col != null) _heroes = col.heroes;
    }

    void ShowFeedback()
    {
        feedbackMsg.AddToClassList("feedback-msg--visible");
        StartCoroutine(HideFeedback());
    }

    IEnumerator HideFeedback()
    {
        yield return new WaitForSeconds(2f);
        feedbackMsg.RemoveFromClassList("feedback-msg--visible");
    }

    // ── UTILIDADES ───────────────────────────────────
    Texture2D LoadTexture(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        return tex;
    }
}