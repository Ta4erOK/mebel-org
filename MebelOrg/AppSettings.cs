namespace MebelOrg
{
    /// <summary>Настройки приложения: строка подключения к базе данных MebelOrg.</summary>
    public static class AppSettings
    {
        public const string ConnectionString =
            @"Server=DESKTOP-FINIK\SQLEXPRESS;Database=MebelOrg;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";
    }
}
