
namespace TechClasses;
public static class Feedback
{
    //Ввод данных игроком
    public static int AnswerPlayerInt()
    {
        Color.CyanShort("Ваш ответ: ");
        int.TryParse(Console.ReadLine(), out int answer);
        Console.WriteLine();
        return answer;
    }
    public static string AnswerPlayerString()
    {
        Color.CyanShort("Ваш ответ: ");
        string answer = Console.ReadLine();
        Console.WriteLine();
        return answer;
    }

    //ожидание нажатия клавиши
    public static void AcceptPlayer()
    {
        Console.WriteLine();
        Console.WriteLine($"Нажмите клавишу для продолжения.");
        Console.ReadKey();
    }

    // показать вопрос и варианты ответов
    public static int[] ShowQuestionAnswers (string question, string[] answers, params string[] returnAnswers)
    {
        int number = 0;
        int returnNumber = 0;
        List<int> returnRange = new List<int>();

        Color.Cyan(question); //вопрос

        foreach (var ans in answers)
        {
           if (ans != null) Console.WriteLine($"[{++number}]. {ans}."); // перечисление ответов
        }

        if (returnAnswers.Length > 0)
        {            
            foreach (var ans in returnAnswers)
            {
                Console.WriteLine($"[{--returnNumber}]. {ans}."); //перечисление ответов на возврат в меню (со знаком минус)
                returnRange.Add(returnNumber);
            }
        }
        return returnRange.ToArray<int>();
    }
}
