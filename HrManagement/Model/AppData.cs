namespace HrManagement.Model;

internal static class AppData
{
    private static readonly Lazy<HrManagementDbContext> db = new(() => new HrManagementDbContext());

    public static HrManagementDbContext Db => db.Value;
}
