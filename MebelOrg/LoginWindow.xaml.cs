using System;
using System.Linq;
using System.Windows;
using MebelOrg.Models;
using Microsoft.EntityFrameworkCore;

namespace MebelOrg;

/// <summary>Окно авторизации: вход по логину и паролю из БД либо гостевой режим.</summary>
public partial class LoginWindow : Window
{
    /// <summary>Авторизованный пользователь (null в гостевом режиме).</summary>
    public User SessionUser { get; private set; }

    /// <summary>Признак гостевого режима.</summary>
    public bool IsGuest { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (sender, args) => LoginBox.Focus();
    }

    /// <summary>Проверка логина и пароля по таблице Users.</summary>
    private void LoginClick(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string password = PasswordBox.Password;

        if (login.Length == 0 || password.Length == 0)
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        try
        {
            using MebelOrgContext context = new MebelOrgContext();
            User found = context.Users
                .Include(user => user.Role)
                .FirstOrDefault(user => user.Login == login && user.Password == password);

            if (found == null)
            {
                ShowError("Неверный логин или пароль.");
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            SessionUser = found;
            IsGuest = false;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowError("Не удалось подключиться к базе данных." + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>Гостевой режим: просмотр каталога без учётной записи.</summary>
    private void GuestClick(object sender, RoutedEventArgs e)
    {
        SessionUser = null;
        IsGuest = true;
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
    }
}
