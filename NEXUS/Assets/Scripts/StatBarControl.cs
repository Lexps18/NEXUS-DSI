using UnityEngine;
using UnityEngine.UIElements;

public class StatBarControl : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StatBarControl, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlIntAttributeDescription m_Value =
            new UxmlIntAttributeDescription { name = "value", defaultValue = 50 };
        UxmlStringAttributeDescription m_Color =
            new UxmlStringAttributeDescription { name = "bar-color", defaultValue = "#C9A84C" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var bar = (StatBarControl)ve;
            bar.Value = m_Value.GetValueFromBag(bag, cc);
            bar.BarColor = m_Color.GetValueFromBag(bag, cc);
        }
    }

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

    public string BarColor { get; set; } = "#C9A84C";

    public StatBarControl()
    {
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.height = 8;
        style.width = new StyleLength(new Length(100, LengthUnit.Percent));

        var bg = new VisualElement();
        bg.style.flexGrow = 1;
        bg.style.height = 8;
        bg.style.backgroundColor = new Color(1, 1, 1, 0.08f);
        bg.style.borderTopLeftRadius = 4;
        bg.style.borderTopRightRadius = 4;
        bg.style.borderBottomLeftRadius = 4;
        bg.style.borderBottomRightRadius = 4;

        _fill = new VisualElement();
        _fill.style.height = 8;
        _fill.style.borderTopLeftRadius = 4;
        _fill.style.borderTopRightRadius = 4;
        _fill.style.borderBottomLeftRadius = 4;
        _fill.style.borderBottomRightRadius = 4;
        _fill.style.position = Position.Absolute;

        bg.Add(_fill);
        Add(bg);
        UpdateBar();
    }

    void UpdateBar()
    {
        if (_fill == null) return;
        float pct = _value / 100f;
        _fill.style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));
        if (ColorUtility.TryParseHtmlString(BarColor, out Color c))
            _fill.style.backgroundColor = c;
    }
}