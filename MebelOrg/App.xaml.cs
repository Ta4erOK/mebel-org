using System.Windows;

namespace MebelOrg
{
    /// <summary>Точка входа приложения. Экраны подключаются на следующих этапах.</summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Shutdown();
        }
    }
}
