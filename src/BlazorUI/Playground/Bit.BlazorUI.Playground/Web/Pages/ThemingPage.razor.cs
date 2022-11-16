namespace Bit.BlazorUI.Playground.Web.Pages;

public partial class ThemingPage
{
    public int PrimaryCounter;
    public int StandardCounter;

    private readonly string example1HTMLCode = @"
<BitButton IsEnabled=""true""
           OnClick=""() => PrimaryCounter++"">
    Primary (@PrimaryCounter)
</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard""
           Style=""margin-right: 2px;""
           IsEnabled=""true""
           OnClick=""() => StandardCounter++"">
    Standard (@StandardCounter)
</BitButton>
<BitButton IsEnabled=""false""
           AllowDisabledFocus=""false"">
    Disabled
</BitButton>
<BitButton Class=""label-btn""
           IsEnabled=""true"">
    <label>A Text from label element</label>
</BitButton>";
}
