namespace TMS_WPF_UI.Helpers
{
    public static class SessionManager
    {
        // This is a deliberately simple in-memory session store for learning. The login
        // flow writes the JWT here, and API-facing view models read it when calling
        // endpoints protected by ASP.NET Core [Authorize].
        public static string? JwtToken { get; set; }
    }
}
