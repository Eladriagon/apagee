namespace Apagee.ViewModels;

public class DashboardViewModel
{
    public int PostCount { get; set; }
    public int BoostCount { get; set; }
    public int FavoriteCount { get; set; }
    public IEnumerable<Models.Article>? RecentArticles { get; set; }
}