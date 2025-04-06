using System.Threading.Tasks;
using Godot;

public partial class Output : PanelContainer
{
    private RichTextLabel content;

    Output()
    {
        Find.Output = this;
    }

    public override void _Ready()
    {
        content = GetNode<RichTextLabel>("%Content");
        GetNode<BaseButton>("%ClearButton").Pressed += content.Clear;

        var copyButton = GetNode<Button>("%CopyButton");
        copyButton.Pressed += async () => {
            // TODO

            var regex = new RegEx();
            GD.Print(content.Text.Length);
            DisplayServer.ClipboardSet(regex.Sub(content.Text, "", true));

            copyButton.Text = "    Copied!    ";
            await Task.Delay(2000);
            copyButton.Text = "    Copy to Clipboard    ";
        };
    }

    public void Print(string text)
    {
        content.AddText(text.EndsWith('\n') ? text : text + "\n");
    }

    public void Error(string text)
    {
        content.PushColor(Color.FromHtml("f76253"));
        content.AddText(text.EndsWith('\n') ? text : text + "\n");
        content.Pop();
    }
}
