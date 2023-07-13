
namespace TechClasses;
public static class Color
{
    //Методы для окрашивания шрифта
    public static void Red(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void RedShort(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void Green(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void GreenShort(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void Cyan(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void CyanShort(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(text);
        Console.ResetColor();
    }
}
