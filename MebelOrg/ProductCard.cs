using System;
using System.Globalization;
using System.Windows.Media;
using MebelOrg.Models;

namespace MebelOrg;

/// <summary>
/// Карточка товара для списка: готовые к отображению тексты, фото и цвета.
/// Оформление задаётся приоритетом: нет на складе -> серая строка;
/// скидка больше 15% -> фон #008080; иначе обычная белая строка.
/// </summary>
public class ProductCard
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly Brush BrushText = Frozen("#001A33");
    private static readonly Brush BrushMuted = Frozen("#5A7A8A");
    private static readonly Brush BrushWhite = Frozen("#FFFFFF");
    private static readonly Brush BrushLightMuted = Frozen("#DFF6F6");
    private static readonly Brush BrushOutText = Frozen("#6E6E6E");
    private static readonly Brush BrushOldPrice = Frozen("#C62828");
    private static readonly Brush BrushOldPriceOnDark = Frozen("#FFB4B4");

    private static readonly Brush BgNormal = Frozen("#FFFFFF");
    private static readonly Brush BgDiscount = Frozen("#008080");
    private static readonly Brush BgOutOfStock = Frozen("#DCDCDC");

    public ProductCard(Product product)
    {
        Product = product;

        bool hasDiscount = product.Discount > 0m;
        bool isBigDiscount = product.Discount > 15m;
        bool outOfStock = product.Quantity <= 0;

        FinalPrice = hasDiscount
            ? Math.Round(product.Price * (1m - product.Discount / 100m), 2)
            : product.Price;

        Photo = ImageStorage.LoadImage(product.ImagePath);

        // Приоритет оформления: отсутствие на складе важнее скидки.
        if (outOfStock)
        {
            RowBackground = BgOutOfStock;
            PrimaryBrush = BrushOutText;
            SecondaryBrush = BrushOutText;
            OldPriceBrush = BrushOutText;
        }
        else if (isBigDiscount)
        {
            RowBackground = BgDiscount;
            PrimaryBrush = BrushWhite;
            SecondaryBrush = BrushLightMuted;
            OldPriceBrush = BrushOldPriceOnDark;
        }
        else
        {
            RowBackground = BgNormal;
            PrimaryBrush = BrushText;
            SecondaryBrush = BrushMuted;
            OldPriceBrush = BrushOldPrice;
        }

        Title = product.Name;
        ArticleLine = "Артикул: " + product.Article;
        CategoryLine = "Категория: " + (product.Category?.Name ?? "—");
        ManufacturerLine = "Производитель: " + (product.Manufacturer?.Name ?? "—");
        SupplierLine = "Поставщик: " + (product.Supplier?.Name ?? "—");
        UnitLine = "Единица измерения: " + (product.Unit?.Name ?? "—");
        Description = string.IsNullOrWhiteSpace(product.Description) ? string.Empty : product.Description.Trim();

        OldPriceText = product.Price.ToString("N2", Ru) + " \u20BD";
        FinalPriceText = FinalPrice.ToString("N2", Ru) + " \u20BD";
        OldPriceVisibility = hasDiscount ? "Visible" : "Collapsed";

        DiscountValueText = hasDiscount ? product.Discount.ToString("0.##", Ru) + " %" : "—";
        DiscountHintText = hasDiscount ? "цена снижена" : "без скидки";
        DiscountText = hasDiscount ? "Скидка: " + DiscountValueText : "Без скидки";

        StockText = outOfStock
            ? "Нет на складе"
            : "На складе: " + product.Quantity.ToString(Ru) + " " + (product.Unit?.Name ?? "шт.");

        SearchText = string.Join(" ",
            product.Article,
            product.Name,
            product.Category?.Name,
            product.Description,
            product.Manufacturer?.Name,
            product.Supplier?.Name,
            product.Unit?.Name).ToLowerInvariant();
    }

    public Product Product { get; }

    public ImageSource Photo { get; }

    public string Title { get; }
    public string ArticleLine { get; }
    public string CategoryLine { get; }
    public string ManufacturerLine { get; }
    public string SupplierLine { get; }
    public string UnitLine { get; }
    public string Description { get; }

    public decimal FinalPrice { get; }
    public string FinalPriceText { get; }
    public string OldPriceText { get; }
    public string OldPriceVisibility { get; }
    public string DiscountText { get; }
    public string DiscountValueText { get; }
    public string DiscountHintText { get; }
    public string StockText { get; }

    public Brush RowBackground { get; }
    public Brush PrimaryBrush { get; }
    public Brush SecondaryBrush { get; }
    public Brush OldPriceBrush { get; }

    /// <summary>Текст для поиска: все текстовые атрибуты товара в нижнем регистре.</summary>
    public string SearchText { get; }

    public double SortPrice => (double)FinalPrice;
    public int SortQuantity => Product.Quantity;
    public decimal SortDiscount => Product.Discount;

    private static Brush Frozen(string hex)
    {
        SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
