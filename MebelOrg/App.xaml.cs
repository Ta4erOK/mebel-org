using System.Windows;

namespace MebelOrg;

/// <summary>
/// Точка входа приложения. Порядок экранов:
/// окно входа -> список товаров -> (выход) -> снова окно входа либо закрытие программы.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ShowLogin();
    }

    /// <summary>Показывает окно авторизации, затем главное окно со списком товаров.</summary>
    private void ShowLogin()
    {
        LoginWindow login = new LoginWindow();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        ProductListWindow list = new ProductListWindow(login.SessionUser, login.IsGuest);
        list.Closed += (sender, args) =>
        {
            if (list.LogoutRequested)
            {
                ShowLogin();
                return;
            }

            Shutdown();
        };
        list.Show();
    }
}
