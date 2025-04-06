using Godot;

public partial class Tooltip : Control
{
    public GodotObject focusedOn;

    private Label content;
    
    Tooltip()
    {
        Find.Tooltip = this;
    }

    public void SetText(string text)
    {
        GetNode<RichTextLabel>("%Content").Text = text;
    }

    public void SetPosition(Vector2 position)
    {
        GetNode<Control>("%Panel").Position = position;
    }
}
