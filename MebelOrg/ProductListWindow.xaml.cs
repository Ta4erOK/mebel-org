using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using MebelOrg.Models;
using Microsoft.EntityFrameworkCore;

namespace MebelOrg;

/// <summary>
/// Главное окно со списком товаров.
/// Гость и авторизованный клиент видят только каталог,
/// менеджер и администратор дополнительно получают поиск, фильтр и сортировку.
/// </summary>
public partial class ProductListWindow : Window
{
    private readonly User _user;
    private readonly bool _isGuest;
    private readonly string _roleName;
    private readonly ObservableCollection<ProductCard> _cards = new ObservableCollection<ProductCard>();
    private readonly ICollectionView _view;

    /// <summary>Признак того, что пользователь нажал «Выход» (нужно вернуть экран входа).</summary>
    public bool LogoutRequested { get; private set; }

    public ProductListWindow(User user, bool isGuest)
    {
        InitializeComponent();

        _user = user;
        _isGuest = isGuest;
        _roleName = user?.Role?.Name?.Trim() ?? string.Empty;

        // Два слоя: коллекция карточек и представление с фильтром и сортировкой
        _view = CollectionViewSource.GetDefaultView(_cards);
        _view.Filter = FilterCard;
        ProductsList.ItemsSource = _view;

        SetupHeader();
        LoadProducts();
        ApplySort();
    }

    /// <summary>Менеджер и администратор могут пользоваться поиском, фильтром и сортировкой.</summary>
    public bool CanManage => _user != null && (IsRole("Менеджер") || IsRole("Администратор"));

    /// <summary>Только администратор может изменять товары (модуль 3).</summary>
    public bool IsAdmin => _user != null && IsRole("Администратор");

    private bool IsRole(string name)
    {
        return string.Equals(_roleName, name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>ФИО в правом верхнем углу и видимость панели фильтров.</summary>
    private void SetupHeader()
    {
        if (_isGuest)
        {
            UserText.Text = "Гость (только просмотр)";
        }
        else
        {
            UserText.Text = _user.FullName + " · " + _roleName;
        }

        FilterPanel.Visibility = CanManage ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Загрузка товаров из базы данных вместе со справочниками.</summary>
    private void LoadProducts()
    {
        try
        {
            using MebelOrgContext context = new MebelOrgContext();
            var products = context.Products
                .Include(product => product.Category)
                .Include(product => product.Manufacturer)
                .Include(product => product.Supplier)
                .Include(product => product.Unit)
                .OrderBy(product => product.Id)
                .ToList();

            _cards.Clear();
            foreach (Product product in products)
            {
                _cards.Add(new ProductCard(product));
            }

            UpdatePositionsFooter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Не удалось загрузить список товаров." + Environment.NewLine + ex.Message,
                "Ошибка загрузки",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Фильтр представления: диапазон скидки, затем поиск по всем текстовым полям.
    /// Каждое слово запроса должно встречаться в карточке товара.
    /// </summary>
    private bool FilterCard(object item)
    {
        ProductCard card = item as ProductCard;
        if (card == null)
        {
            return false;
        }

        decimal discount = card.SortDiscount;
        switch (DiscountBox.SelectedIndex)
        {
            case 1:
                if (discount >= 11m)
                {
                    return false;
                }
                break;
            case 2:
                if (discount < 11m || discount >= 15m)
                {
                    return false;
                }
                break;
            case 3:
                if (discount < 15m)
                {
                    return false;
                }
                break;
        }

        if (!CanManage)
        {
            return true;
        }

        string query = SearchBox.Text.Trim().ToLowerInvariant();
        if (query.Length == 0)
        {
            return true;
        }

        string[] words = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string word in words)
        {
            if (!card.SearchText.Contains(word))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Сортировка списка по выбранному пункту.</summary>
    private void ApplySort()
    {
        if (_view == null)
        {
            return;
        }

        _view.SortDescriptions.Clear();

        switch (SortBox.SelectedIndex)
        {
            case 1:
                _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortPrice), ListSortDirection.Ascending));
                break;
            case 2:
                _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortPrice), ListSortDirection.Descending));
                break;
            case 3:
                _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortQuantity), ListSortDirection.Ascending));
                break;
            case 4:
                _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortQuantity), ListSortDirection.Descending));
                break;
            case 5:
                _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.SortDiscount), ListSortDirection.Descending));
                break;
            default:
                _view.SortDescriptions.Add(new SortDescription(nameof(ProductCard.Title), ListSortDirection.Ascending));
                break;
        }

        _view.Refresh();
    }

    /// <summary>Счётчик позиций в футере окна.</summary>
    private void UpdatePositionsFooter()
    {
        if (_view == null)
        {
            return;
        }

        int total = _cards.Count;
        int shown = _view.Cast<object>().Count();

        PositionsText.Text = shown == total
            ? "Всего позиций: " + total
            : "Показано позиций: " + shown + " из " + total;
    }

    private void SearchBoxTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_view == null)
        {
            return;
        }

        _view.Refresh();
        UpdatePositionsFooter();
    }

    private void DiscountChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_view == null)
        {
            return;
        }

        _view.Refresh();
        UpdatePositionsFooter();
    }

    private void SortChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_view == null)
        {
            return;
        }

        ApplySort();
        UpdatePositionsFooter();
    }

    private void ResetClick(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        DiscountBox.SelectedIndex = 0;
        SortBox.SelectedIndex = 0;
        _view.Refresh();
        UpdatePositionsFooter();
    }

    /// <summary>Возврат на экран входа (обрабатывается в App.xaml.cs).</summary>
    private void LogoutClick(object sender, RoutedEventArgs e)
    {
        LogoutRequested = true;
        Close();
    }
}
