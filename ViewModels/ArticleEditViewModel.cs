namespace Apagee.ViewModels;

public class ArticleEditViewModel
{
    public string? Uid { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public bool WasEverPublished { get; set; }
}