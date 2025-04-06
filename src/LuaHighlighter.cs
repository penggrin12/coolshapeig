using Godot;

[Tool, GlobalClass]
public partial class LuaHighlighter : CodeHighlighter
{
    private static readonly Color TEXT_COLOR = Color.FromHtml("70FFBB");
    private static readonly Color COMMENT_COLOR = Color.FromHtml("878787");

    private static readonly Color NUMBER_COLOR = Color.FromHtml("9e86c8");
    private static readonly Color SYMBOL_COLOR = Color.FromHtml("70bbff");
    private static readonly Color FUNCTION_COLOR = Color.FromHtml("ff983f");
    private static readonly Color MEMBER_COLOR = Color.FromHtml("70bbff");

    LuaHighlighter()
    {
        KeywordColors = [];
        MemberKeywordColors = [];
        ColorRegions = [];


        AddKeywordColors([
            "and", "break", "do", "else", "elseif", "end", "false", "for",
            "function", "local", "if", "in", "local", "nil", "not", "or",
            "repeat", "return", "then", "true", "until", "while",
        ]);

        AddColorRegion("\"", "\"", TEXT_COLOR);
        AddColorRegion("\'", "\'", TEXT_COLOR);
        AddColorRegion("[[", "]]", TEXT_COLOR);
        AddColorRegion("--[[", "]]", COMMENT_COLOR);
        AddColorRegion("--", "", COMMENT_COLOR, true);
    }

    private void AddKeywordColors(string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            AddKeywordColor(keyword, SYMBOL_COLOR);
        }
    }
}
