using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class NexusController : MonoBehaviour
{
    private List<HeroData> _heroes = new();
    private HeroData _selected;
    private int _currentLevel = 1;
    private string _pickedImagePath = "";

    private VisualElement root;

    private Button tabHeroes, tabNew, tabStats;
    private VisualElement panelHeroes, panelNew, panelStats;

    private TextField searchField;
    private VisualElement heroGrid;
    private VisualElement detailPanel;
    private Label detailInitials, detailName, detailSub;
    private StatBarControl barVida, barAtk, barDef;
    private Label valVida, valAtk, valDef;
    private Button btnEdit, btnDelete;

    private TextField inputName;
    private DropdownField inputClass;
    private Label levelDisplay;
    private Button btnLevelUp, btnLevelDown;
    private SliderInt inputVida, inputAtk, inputDef;
    private Label valInputVida, valInputAtk, valInputDef;
    private Button btnPickImage, btnExamineImage, btnAdd;
    private Label imageNameLabel;
    private VisualElement imagePreview;

    private Label valTotal, valAvgLevel, valBest, valBestSub, valPower;
    private VisualElement tableBody;

    private Label feedbackMsg;
    private Button btnSave;

    private int _currentPage = 0;
    private const int CardsPerPage = 5;
    private Label heroCounter;
    private Button btnPrev, btnNext;

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
        tabHeroes = root.Q<Button>("tab-heroes");
        tabNew = root.Q<Button>("tab-new");
        tabStats = root.Q<Button>("tab-stats");
        panelHeroes = root.Q<VisualElement>("panel-heroes");
        panelNew = root.Q<VisualElement>("panel-new");
        panelStats = root.Q<VisualElement>("panel-stats");

        tabHeroes.clicked += () => SwitchTab(0);
        tabNew.clicked += () => SwitchTab(1);
        tabStats.clicked += () => SwitchTab(2);

        btnSave = root.Q<Button>("btn-save");
        btnSave.clicked += SaveToJson;

        searchField = root.Q<TextField>("search-field");
        searchField.RegisterValueChangedCallback(e => { _currentPage = 0; RefreshHeroGrid(); });

        heroGrid = root.Q<VisualElement>("hero-grid");
        detailPanel = root.Q<VisualElement>("detail-panel");
        detailInitials = root.Q<Label>("detail-initials");
        detailName = root.Q<Label>("detail-name");
        detailSub = root.Q<Label>("detail-sub");
        barVida = root.Q<StatBarControl>("bar-vida");
        barAtk = root.Q<StatBarControl>("bar-atk");
        barDef = root.Q<StatBarControl>("bar-def");
        valVida = root.Q<Label>("val-vida");
        valAtk = root.Q<Label>("val-atk");
        valDef = root.Q<Label>("val-def");
        btnEdit = root.Q<Button>("btn-edit");
        btnDelete = root.Q<Button>("btn-delete");

        heroCounter = root.Q<Label>("hero-counter");
        btnPrev = root.Q<Button>("btn-prev");
        btnNext = root.Q<Button>("btn-next");
        btnPrev.clicked += () => { _currentPage--; RefreshHeroGrid(); };
        btnNext.clicked += () => { _currentPage++; RefreshHeroGrid(); };

        btnEdit.clicked += OnEditClicked;
        btnDelete.clicked += OnDeleteClicked;

        detailPanel.AddToClassList("detail-panel--hidden");

        inputName = root.Q<TextField>("input-name");
        inputClass = root.Q<DropdownField>("input-class");
        levelDisplay = root.Q<Label>("level-display");
        btnLevelUp = root.Q<Button>("btn-level-up");
        btnLevelDown = root.Q<Button>("btn-level-down");
        inputVida = root.Q<SliderInt>("input-vida");
        inputAtk = root.Q<SliderInt>("input-atk");
        inputDef = root.Q<SliderInt>("input-def");

        valInputVida = root.Q<Label>("val-input-vida");
        valInputAtk = root.Q<Label>("val-input-atk");
        valInputDef = root.Q<Label>("val-input-def");

        inputVida.RegisterValueChangedCallback(e => valInputVida.text = e.newValue.ToString());
        inputAtk.RegisterValueChangedCallback(e => valInputAtk.text = e.newValue.ToString());
        inputDef.RegisterValueChangedCallback(e => valInputDef.text = e.newValue.ToString());

        btnPickImage = root.Q<Button>("btn-pick-image");
        btnExamineImage = root.Q<Button>("btn-examine-image");
        imageNameLabel = root.Q<Label>("image-name-label");
        imagePreview = root.Q<VisualElement>("image-preview");
        btnAdd = root.Q<Button>("btn-add");

        btnLevelUp.clicked += () => ChangeLevel(1);
        btnLevelDown.clicked += () => ChangeLevel(-1);
        btnPickImage.clicked += PickImage;
        btnExamineImage.clicked += ShowImageModal;
        btnAdd.clicked += OnAddHero;

        valTotal = root.Q<Label>("val-total");
        valAvgLevel = root.Q<Label>("val-avg-level");
        valBest = root.Q<Label>("val-best");
        valBestSub = root.Q<Label>("val-best-sub");
        valPower = root.Q<Label>("val-power");
        tableBody = root.Q<VisualElement>("table-body");

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

        var filtered = new List<HeroData>();
        foreach (var h in _heroes)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !h.nombre.ToLower().Contains(filter) &&
                !h.clase.ToLower().Contains(filter))
                continue;
            filtered.Add(h);
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)CardsPerPage));
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

        int start = _currentPage * CardsPerPage;
        int end = Mathf.Min(start + CardsPerPage, filtered.Count);

        for (int i = start; i < end; i++)
            heroGrid.Add(CreateCard(filtered[i]));

        if (heroCounter != null)
            heroCounter.text = $"{filtered.Count} heroes  |  {_currentPage + 1}/{totalPages}";

        if (btnPrev != null)
            btnPrev.style.opacity = _currentPage > 0 ? 1f : 0.3f;
        if (btnNext != null)
            btnNext.style.opacity = _currentPage < totalPages - 1 ? 1f : 0.3f;
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

        var cardRoot = card.Q<VisualElement>("hero-card");
        cardRoot.RegisterCallback<ClickEvent>(e => SelectHero(h));

        return card;
    }

    // ── OBSERVER ──────────────────────────────────────
    void SelectHero(HeroData h)
    {
        _selected = h;
        detailPanel.RemoveFromClassList("detail-panel--hidden");

        foreach (var c in heroGrid.Children())
            c.Q<VisualElement>("hero-card")?.RemoveFromClassList("hero-card--selected");

        foreach (var c in heroGrid.Children())
        {
            var cr = c.Q<VisualElement>("hero-card");
            if (cr?.Q<Label>("card-name")?.text == h.nombre)
                cr.AddToClassList("hero-card--selected");
        }

        detailInitials.text = h.Iniciales();
        detailName.text = h.nombre;
        detailSub.text = $"{h.clase}  ·  Nivel {h.nivel}";

        barVida.Value = h.vida; barVida.UpdateBar();
        barAtk.Value = h.ataque; barAtk.UpdateBar();
        barDef.Value = h.defensa; barDef.UpdateBar();

        valVida.text = h.vida.ToString();
        valAtk.text = h.ataque.ToString();
        valDef.text = h.defensa.ToString();

        var avatar = root.Q<VisualElement>("detail-avatar");
        if (!string.IsNullOrEmpty(h.imagenPath) && File.Exists(h.imagenPath))
        {
            var tex = LoadTexture(h.imagenPath);
            if (tex != null)
            {
                avatar.style.backgroundImage = new StyleBackground(tex);
                avatar.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
                detailInitials.style.display = DisplayStyle.None;
            }
        }
        else
        {
            avatar.style.backgroundImage = StyleKeyword.Null;
            detailInitials.style.display = DisplayStyle.Flex;
        }
    }

    // ── EDITAR / ELIMINAR ─────────────────────────────
    void OnEditClicked()
    {
        if (_selected == null) return;
        var editando = _selected;
        _heroes.Remove(_selected);
        _selected = null;
        detailPanel.AddToClassList("detail-panel--hidden");
        RefreshHeroGrid();
        SwitchTab(1);

        inputName.value = editando.nombre;
        int claseIdx = inputClass.choices.IndexOf(editando.clase);
        inputClass.index = claseIdx >= 0 ? claseIdx : 0;
        _currentLevel = editando.nivel;
        levelDisplay.text = _currentLevel.ToString();
        inputVida.value = editando.vida;
        inputAtk.value = editando.ataque;
        inputDef.value = editando.defensa;
        valInputVida.text = editando.vida.ToString();
        valInputAtk.text = editando.ataque.ToString();
        valInputDef.text = editando.defensa.ToString();
        _pickedImagePath = editando.imagenPath ?? "";
        imageNameLabel.text = string.IsNullOrEmpty(_pickedImagePath)
            ? "Ninguna seleccionada" : Path.GetFileName(_pickedImagePath);

        if (!string.IsNullOrEmpty(_pickedImagePath) && File.Exists(_pickedImagePath))
        {
            var tex = LoadTexture(_pickedImagePath);
            if (tex != null && imagePreview != null)
            {
                imagePreview.style.backgroundImage = new StyleBackground(tex);
                imagePreview.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
                var ph = imagePreview.Q<Label>();
                if (ph != null) ph.style.display = DisplayStyle.None;
            }
        }
    }

    void OnDeleteClicked()
    {
        if (_selected == null) return;
        _heroes.Remove(_selected);
        _selected = null;
        detailPanel.AddToClassList("detail-panel--hidden");
        RefreshHeroGrid();
    }

    // ── FORMULARIO ────────────────────────────────────
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
        if (valInputVida != null) valInputVida.text = "50";
        if (valInputAtk != null) valInputAtk.text = "50";
        if (valInputDef != null) valInputDef.text = "50";
        _pickedImagePath = "";
        if (imageNameLabel != null) imageNameLabel.text = "Ninguna seleccionada";
        if (imagePreview != null)
        {
            imagePreview.style.backgroundImage = StyleKeyword.Null;
            var ph = imagePreview.Q<Label>();
            if (ph != null) ph.style.display = DisplayStyle.Flex;
        }
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
                var ph = imagePreview.Q<Label>();
                if (ph != null) ph.style.display = DisplayStyle.None;
            }
        }
#endif
    }

    void ShowImageModal()
    {
        if (string.IsNullOrEmpty(_pickedImagePath) || !File.Exists(_pickedImagePath)) return;
        var tex = LoadTexture(_pickedImagePath);
        if (tex == null) return;

        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.85f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var imgEl = new VisualElement();
        imgEl.style.width = 500; imgEl.style.height = 500;
        imgEl.style.backgroundImage = new StyleBackground(tex);
        imgEl.style.backgroundSize =
            new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        imgEl.style.borderTopLeftRadius = 12; imgEl.style.borderTopRightRadius = 12;
        imgEl.style.borderBottomLeftRadius = 12; imgEl.style.borderBottomRightRadius = 12;

        var btnClose = new Button(() => root.Remove(overlay));
        btnClose.text = "Cerrar";
        btnClose.style.marginTop = 16;
        btnClose.style.backgroundColor = new Color(0.79f, 0.66f, 0.30f);
        btnClose.style.color = new Color(0.05f, 0.06f, 0.08f);
        btnClose.style.borderTopWidth = 0; btnClose.style.borderBottomWidth = 0;
        btnClose.style.borderLeftWidth = 0; btnClose.style.borderRightWidth = 0;
        btnClose.style.borderTopLeftRadius = 8; btnClose.style.borderTopRightRadius = 8;
        btnClose.style.borderBottomLeftRadius = 8; btnClose.style.borderBottomRightRadius = 8;
        btnClose.style.paddingTop = 10; btnClose.style.paddingBottom = 10;
        btnClose.style.paddingLeft = 32; btnClose.style.paddingRight = 32;
        btnClose.style.fontSize = 14;
        btnClose.style.unityFontStyleAndWeight = FontStyle.Bold;

        overlay.Add(imgEl);
        overlay.Add(btnClose);
        root.Add(overlay);
        overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) root.Remove(overlay); });
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
        _currentPage = (_heroes.Count - 1) / CardsPerPage;
        RefreshHeroGrid();
        SwitchTab(0);
        SelectHero(h);
    }

    // ── STATS ─────────────────────────────────────────
    void RefreshStats()
    {
        valTotal.text = _heroes.Count.ToString();
        if (_heroes.Count == 0)
        {
            valAvgLevel.text = "0"; valBest.text = "-";
            valBestSub.text = ""; valPower.text = "0";
            tableBody.Clear(); return;
        }

        float avg = 0; int totalPow = 0;
        HeroData best = _heroes[0];
        foreach (var h in _heroes)
        {
            avg += h.nivel; totalPow += h.Poder;
            if (h.Poder > best.Poder) best = h;
        }

        valAvgLevel.text = (avg / _heroes.Count).ToString("F1");
        valBest.text = best.nombre;
        valBestSub.text = $"{best.clase} · Nivel {best.nivel}";
        valPower.text = totalPow.ToString();

        tableBody.Clear();
        var sorted = new List<HeroData>(_heroes);
        sorted.Sort((a, b) => b.Poder.CompareTo(a.Poder));

        int maxRows = Mathf.Min(5, sorted.Count);
        for (int i = 0; i < maxRows; i++)
        {
            var h = sorted[i];
            var row = new VisualElement();
            row.AddToClassList("table-row");

            var nombre = new Label(h.nombre);
            nombre.AddToClassList("table-col-nombre");
            nombre.style.fontSize = 13;
            nombre.style.color = new Color(0.91f, 0.91f, 0.94f);

            var badge = new Label(h.clase.ToUpper());
            badge.AddToClassList("badge");
            badge.AddToClassList(h.BadgeClass());
            badge.AddToClassList("table-col-clase");
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;

            var nivel = new Label(h.nivel.ToString());
            nivel.AddToClassList("table-col-nivel");
            nivel.style.fontSize = 13;
            nivel.style.color = new Color(0.91f, 0.91f, 0.94f);
            nivel.style.unityTextAlign = TextAnchor.MiddleCenter;

            var poder = new Label(h.Poder.ToString());
            poder.AddToClassList("table-col-poder");
            poder.style.fontSize = 13;
            poder.style.color = new Color(0.79f, 0.66f, 0.30f);
            poder.style.unityFontStyleAndWeight = FontStyle.Bold;
            poder.style.unityTextAlign = TextAnchor.MiddleCenter;

            row.Add(nombre); row.Add(badge);
            row.Add(nivel); row.Add(poder);
            tableBody.Add(row);
        }
    }

    // ── JSON ──────────────────────────────────────────
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

    Texture2D LoadTexture(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        return tex;
    }
}