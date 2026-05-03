using UnityEngine;
using UnityEngine.UIElements;

public class StatBarControl : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StatBarControl, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlIntAttributeDescription m_Value =
            new UxmlIntAttributeDescription { name = "value", defaultValue = 50 };
        UxmlColorAttributeDescription m_Color =
            new UxmlColorAttributeDescription { name = "bar-color", defaultValue = new Color(0.79f, 0.66f, 0.30f) };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var bar = (StatBarControl)ve;
            bar.Value = m_Value.GetValueFromBag(bag, cc);
            bar.BarColor = m_Color.GetValueFromBag(bag, cc);
        }
    }

    private VisualElement _bg;
    private VisualElement _fill;
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            _value = Mathf.Clamp(value, 0, 100);
            UpdateBar();
        }
    }

    public Color BarColor { get; set; } = new Color(0.79f, 0.66f, 0.30f);

    public StatBarControl()
    {
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.flexGrow = 1;
        style.height = 8;

        _bg = new VisualElement();
        _bg.style.flexGrow = 1;
        _bg.style.height = 8;
        _bg.style.backgroundColor = new Color(1, 1, 1, 0.08f);
        _bg.style.borderTopLeftRadius = 4;
        _bg.style.borderTopRightRadius = 4;
        _bg.style.borderBottomLeftRadius = 4;
        _bg.style.borderBottomRightRadius = 4;

        _fill = new VisualElement();
        _fill.style.height = 8;
        _fill.style.position = Position.Absolute;
        _fill.style.borderTopLeftRadius = 4;
        _fill.style.borderTopRightRadius = 4;
        _fill.style.borderBottomLeftRadius = 4;
        _fill.style.borderBottomRightRadius = 4;

        _bg.Add(_fill);
        Add(_bg);
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (_fill == null) return;
        float pct = _value / 100f;
        _fill.style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));
        _fill.style.backgroundColor = BarColor;
    }
}