using Godot;

[GlobalClass]
public partial class ResetableSplitContainer : Node
{
    private SplitContainer splitContainer;

    public override void _Ready()
    {
        splitContainer = GetNode<SplitContainer>("..");
        splitContainer.GetDragAreaControl().GuiInput += @event =>
        {
            if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.ButtonIndex == MouseButton.Right && mouseButtonEvent.Pressed)
            {
                splitContainer.SplitOffset = 0;
            }
        };
    }
}
