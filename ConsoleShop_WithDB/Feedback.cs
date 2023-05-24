
using System.Linq;

namespace ConsoleShop_WithDB
{
    internal static class Feedback
    {

        //проверка условий на ввод данных игроком
        internal static bool CheckСonditions(int answerInput, int MaxRange, int MinRange, params int[] exeptions)
        {

            if (!exeptions.Contains(answerInput))
            {
                if (!(answerInput >= MinRange && answerInput <= MaxRange))
                {
                    Color.Red("Введенное значение неверно.");
                    Console.WriteLine();
                    return false;
                }
            }
            return true;
        }
        internal static bool CheckСonditionsString(string answerInput, params int[] exeptions)
        {
            int.TryParse(answerInput, out int exeption);

            if (answerInput == null && !exeptions.Contains(exeption))
            {
                Color.Red("Введенное значение неверно.");
                Console.WriteLine();
                return false;
            }
            return true;
        }

        //Ввод данных игроком
        internal static int PlayerAnswer()
        {
            Color.CyanShort("Ваш ответ: ");
            int.TryParse(Console.ReadLine(), out int answer);
            Console.WriteLine();
            return answer;
        }
        internal static string PlayerAnswerString()
        {
            Color.CyanShort("Ваш ответ: ");
            string answer = Console.ReadLine();
            Console.WriteLine();
            return answer;
        }

    }
}
