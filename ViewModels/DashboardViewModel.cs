namespace Apagee.ViewModels;

public class DashboardViewModel
{
    public uint FollowerCount { get; set; }
    public int PostCount { get; set; }
    public int BoostCount { get; set; }
    public int FavoriteCount { get; set; }
    public ArticleListViewModel? RecentArticles { get; set; }
}