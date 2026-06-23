namespace BossKey.Services;

public static class HideOperationStatus
{
    public static string Create(int hidden, int restrictedCount, bool showAdminHint)
    {
        if (hidden == 0)
        {
            return restrictedCount > 0 && showAdminHint
                ? "没有窗口被隐藏；选中的窗口可能需要以管理员身份运行老板键"
                : "没有窗口被隐藏";
        }

        var message = $"已隐藏 {hidden} 个窗口";
        if (restrictedCount > 0 && showAdminHint)
        {
            message += $"，其中 {restrictedCount} 个可能需要管理员权限";
        }

        return message;
    }
}
