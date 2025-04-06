using System;
using System.Linq;
using Godot;
using Lua;

public partial class CodeEditor : Control
{
    [Signal] public delegate void RunPressedEventHandler();

    private CodeEdit codeEdit;
    private Label rowColumn;

    public string Text { get => codeEdit.Text; set => codeEdit.Text = value; }

    public override void _Ready()
    {
        codeEdit = GetNode<CodeEdit>("%CodeEdit");
        codeEdit.CodeCompletionRequested += CodeCompleteionRequested;
        codeEdit.TextChanged += CodeCompleteionRequested;
        codeEdit.LinesEditedFrom += async (fromLine, toLine) => {
            if (!Input.IsActionJustPressed("ui_text_newline"))
                return;

            var caret = new Vector2I(codeEdit.GetCaretColumn(), codeEdit.GetCaretLine());
            var line = codeEdit.Text.Split('\n')[caret.Y].TrimEnd();
            var tabs = line.TakeWhile(c => c == '\t').Count();
            
            if (line.EndsWith("then") || line.EndsWith("do") || line.EndsWith("else") || line.EndsWith("elseif"))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                codeEdit.InsertTextAtCaret($"\t\n{new string('\t', tabs)}end");
                codeEdit.SetCaretLine(caret.Y + 1);
                codeEdit.SetCaretColumn(9999);
            }
        };

        rowColumn = GetNode<Label>("%RowColumnLabel");
        // TODO: doesnt account for selection changes
        codeEdit.CaretChanged += () => {
            if (codeEdit.HasSelection())
                rowColumn.Text = $"{codeEdit.GetSelectionFromLine() + 1},{codeEdit.GetSelectionFromColumn() + 1}-{codeEdit.GetSelectionToLine() + 1},{codeEdit.GetSelectionToColumn() + 1}";
            else
                rowColumn.Text = $"{codeEdit.GetCaretLine() + 1},{codeEdit.GetCaretColumn() + 1}";
        };

        GetNode<BaseButton>("%RunButton").Pressed += () => EmitSignal(SignalName.RunPressed);
        GetNode<BaseButton>("%SaveButton").Pressed += OpenSaveDialog;
        GetNode<BaseButton>("%LoadButton").Pressed += OpenLoadDialog;
        GetNode<BaseButton>("%ClearButton").Pressed += () => { codeEdit.Text = ""; };
    }

    public void CodeCompleteionRequested()
    {
        var constantIcon = GD.Load<Texture>("res://assets/images/MemberConstant.svg");
        var functionIcon = GD.Load<Texture>("res://assets/images/MemberMethod.svg");
        var propertyIcon = GD.Load<Texture>("res://assets/images/MemberProperty.svg");

        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Variable, "x", "x", Colors.Red, icon: propertyIcon);
        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Variable, "y", "y", Colors.Green, icon: propertyIcon);
        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Variable, "z", "z", Colors.DodgerBlue, icon: propertyIcon);

        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Function, "ternary()", "ternary(", icon: functionIcon);

        // math
        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Constant, "math.pi", "math.pi", icon: constantIcon);
        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Constant, "math.huge", "math.huge", icon: constantIcon);
        foreach (LuaFunction luaFunction in Lua.Standard.MathematicsLibrary.Instance.Functions)
        {
            if (LuaManager.BlacklistedFunctions.Contains($"math.{luaFunction.Name}")) continue;
            codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Function, $"math.{luaFunction.Name}()", $"math.{luaFunction.Name}(", icon: functionIcon);
        }

        // std
        foreach (LuaFunction luaFunction in Lua.Standard.BasicLibrary.Instance.Functions)
        {
            if (LuaManager.BlacklistedFunctions.Contains(luaFunction.Name)) continue;
            codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Function, $"{luaFunction.Name}()", $"{luaFunction.Name}(", icon: functionIcon);
        }

        // colors
        codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Function, "Color.new()", "Color.new(", icon: functionIcon);
        foreach ((string colorName, LuaColor color) in LuaManager.BakedColors)
        {
            codeEdit.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Constant, colorName, colorName, color.GodotColor, icon: constantIcon);
        }

        codeEdit.UpdateCodeCompletionOptions(true);
    }

    public void OpenSaveDialog()
    {
        var fd = new FileDialog()
        {
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = true,
            Disable3D = true,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Filters = ["*.lua;Lua Source code"],
        };
        fd.Popup();
        fd.FileSelected += Save;
    }

    public void Save(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

        file.StoreString(codeEdit.Text);
        GD.Print($"saved {path}. ({codeEdit.Text.Length} characters)");
    }
    
    public void OpenLoadDialog()
    {
        var fd = new FileDialog()
        {
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = true,
            Disable3D = true,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Filters = ["*.lua;Lua Source code"],
        };
        fd.Popup();
        fd.FileSelected += Load;
    }

    public void Load(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

        codeEdit.Text = file.GetAsText(true);
        GD.Print($"loaded {path}. ({codeEdit.Text.Length} characters)");
    }
}
