﻿using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitChoiceGroup
{
    private bool isRequired;

    /// <summary>
    /// ID of an element to use as the aria label for this ChoiceGroup.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Default selected key for ChoiceGroup.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// If true, an option must be selected in the ChoiceGroup.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Change direction to RTL.
    /// </summary>
    [Parameter] public bool IsRtl { get; set; }

    /// <summary>
    /// Descriptive label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Used to customize the label for the ChoiceGroup.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// You can define the ChoiceGroup in Horizontal or Vertical mode.
    /// </summary>
    [Parameter] public BitLayoutFlow? LayoutFlow { get; set; }

    /// <summary>
    /// Name of ChoiceGroup, this name is used to group each option into the same logical ChoiceGroup.
    /// </summary>
    [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// List of options, each of which is a selection in the ChoiceGroup.
    /// </summary>
    [Parameter] public IEnumerable<BitChoiceGroupOption> Options { get; set; } = new List<BitChoiceGroupOption>();

    /// <summary>
    /// Used to customize the Option for the ChoiceGroup.
    /// </summary>
    [Parameter] public RenderFragment<BitChoiceGroupOption>? OptionTemplate { get; set; }

    [Parameter] public RenderFragment<BitChoiceGroupOption>? OptionLabelTemplate { get; set; }

    /// <summary>
    /// Callback that is called when the ChoiceGroup value has changed.
    /// </summary>
    [Parameter] public EventCallback<BitChoiceGroupOption> OnChange { get; set; }

    protected override string RootElementClass => "bit-chg";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled && Options.Any(o => o.IsEnabled) && IsRequired
                                   ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsRtl
                                   ? $"{RootElementClass}-rtl-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        if (DefaultValue.HasValue() && Options.Any(o => o.Value == DefaultValue))
        {
            CurrentValue = DefaultValue;
        }

        OnValueChanging += HandleOnValueChanging;

        await base.OnInitializedAsync();
    }

    private string GetGroupLabelId() => $"ChoiceGroupLabel{UniqueId}";

    private string GetGroupAriaLabelledBy() => AriaLabelledBy ?? GetGroupLabelId();

    private string GetOptionInputId(BitChoiceGroupOption option) =>
        option.Id ?? $"ChoiceGroupOptionInput{UniqueId}-{option.Value}";

    private string GetOptionAriaLabel(BitChoiceGroupOption option) =>
      option.AriaLabel ?? AriaLabel ?? string.Empty;

    private bool GetOptionIsChecked(BitChoiceGroupOption option) =>
        CurrentValue.HasValue() && CurrentValue == option.Value;

    private string? GetOptionImageSrc(BitChoiceGroupOption option) =>
        GetOptionIsChecked(option) && option.SelectedImageSrc.HasValue()
        ? option.SelectedImageSrc
        : option.ImageSrc;

    private string GetOptionLabelClassName(BitChoiceGroupOption option) =>
        (option.ImageSrc.HasValue() || option.IconName is not null) && OptionLabelTemplate is null
        ? "bit-chgo-lbl-with-img"
        : "bit-chgo-lbl";

    private static string GetOptionImageSizeStyle(BitChoiceGroupOption option) => option.ImageSize is not null
            ? $"width:{option.ImageSize.Value.Width}px; height:{option.ImageSize.Value.Height}px;"
            : string.Empty;

    private string GetOptionDivClassName(BitChoiceGroupOption option)
    {
        const string itemRootElementClass = "bit-chgo";
        StringBuilder cssClass = new(itemRootElementClass);

        if (OptionTemplate is not null) return cssClass.ToString();

        if (GetOptionIsChecked(option))
        {
            cssClass
                .Append(' ')
                .Append(itemRootElementClass)
                .Append("-checked");
        }

        if (OptionLabelTemplate is not null) return cssClass.ToString();

        if (option.IsEnabled is false || IsEnabled is false)
        {
            cssClass
               .Append(' ')
               .Append(itemRootElementClass)
               .Append("-disabled");
        }

        if (option.ImageSrc.HasValue() || option.IconName is not null)
        {
            cssClass
                .Append(' ')
                .Append(itemRootElementClass)
                .Append("-with-img");
        }

        return cssClass.ToString();
    }

    private async void HandleOptionChange(BitChoiceGroupOption option)
    {
        if (option.IsEnabled is false || IsEnabled is false) return;

        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValue = option.Value;

        await OnChange.InvokeAsync(option);
    }

    private void HandleOnValueChanging(object? sender, ValueChangingEventArgs<string?> args)
    {
        var option = Options.FirstOrDefault(i => i.Value == args.Value);

        if (option is null)
        {
            args.ShouldChange = false;

            CurrentValue = null;

            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
            if (Value == args.Value) return;

            _ = ValueChanged.InvokeAsync(Value);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
