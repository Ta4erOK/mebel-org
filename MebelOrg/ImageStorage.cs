using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MebelOrg;

/// <summary>
/// Работа с изображениями товаров. В базе данных хранятся только имена файлов,
/// сами файлы лежат в папке Images рядом с исполняемым файлом приложения.
/// </summary>
public static class ImageStorage
{
    /// <summary>Файл-заглушка для товаров без фотографии.</summary>
    public const string Placeholder = "picture.png";

    /// <summary>Папка с изображениями рядом с исполняемым файлом.</summary>
    public static string ImagesDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

    /// <summary>Возвращает полный путь к изображению; если файла нет — путь к заглушке.</summary>
    public static string ResolvePath(string relativePath)
    {
        string fileName = string.IsNullOrWhiteSpace(relativePath)
            ? Placeholder
            : Path.GetFileName(relativePath.Trim());

        string fullPath = Path.Combine(ImagesDirectory, fileName);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        string placeholderPath = Path.Combine(ImagesDirectory, Placeholder);
        return File.Exists(placeholderPath) ? placeholderPath : null;
    }

    /// <summary>
    /// Загружает изображение без блокировки файла на диске:
    /// поток читается целиком (BitmapCacheOption.OnLoad) и закрывается, изображение замораживается.
    /// </summary>
    public static ImageSource LoadImage(string relativePath)
    {
        string fullPath = ResolvePath(relativePath);
        if (fullPath == null)
        {
            return null;
        }

        try
        {
            BitmapImage image = new BitmapImage();
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
