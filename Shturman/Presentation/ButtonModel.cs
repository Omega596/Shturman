using System.Net.Mime;

namespace Shturman.Presentation;

public record Button(string Text)
{
    public Button ChangeText() => this with
    {
        Text = Text == "Я заблудился" ? "Остановить" : "Я заблудился"
    };
}
