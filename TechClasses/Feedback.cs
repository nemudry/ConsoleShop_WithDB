
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
    //Ввод данных игроком, строки
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

        Color.Cyan(question); //показать вопрос
        foreach (var answer in answers)// перечисление возможных ответов
            if (answer != null) Console.WriteLine($"[{++number}]. {answer}.");

        //перечисление ответов на возврат в меню (со знаком минус)
        if (returnAnswers.Length > 0)
        {            
            foreach (var ans in returnAnswers)
            {
                Console.WriteLine($"[{--returnNumber}]. {ans}.");
                returnRange.Add(returnNumber);
            }
        }
        return returnRange.ToArray<int>();
    }
}
