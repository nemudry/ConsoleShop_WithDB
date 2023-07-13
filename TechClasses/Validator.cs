using System.Text.RegularExpressions;

namespace TechClasses;
public class Validator
{
    //проверка условий на ввод данных игроком
    public static bool CheckСonditions(int answerInput, int MaxRange, int MinRange, params int[] exeptions)
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
    //проверка условий на ввод данных игроком, строки
    public static bool CheckСonditionsString(string answerInput, Regex regex, params int[] exeptions)
    {
        int.TryParse(answerInput, out int exeption);
        bool result = regex.IsMatch(answerInput);

        if (!result && !exeptions.Contains(exeption))
        {
            Color.Red("Введенное значение неверно.");
            Console.WriteLine();
            return false;
        }
        return true;
    }

    //получение проверенного ответа
    public static int GetChechedAnswer(string question, string[] answers, params string[] returnAnswers)
    {
        int clientAnswer = 0;
        while (true)
        {
            var range = Feedback.ShowQuestionAnswers(question, answers, returnAnswers); //показать вопрос и ответ

            clientAnswer = Feedback.AnswerPlayerInt(); // получение ответа клиента

            if (CheckСonditions(clientAnswer, answers.Length, 1, range)) break; // проверка ответа валидатором
        }
        return clientAnswer;
    }

    //получение проверенного ответа, строки
    public static string GetChechedAnswerString(string question, Regex regex, params string[] returnAnswers)
    {
        string clientAnswer;
        while (true)
        {
            var range = Feedback.ShowQuestionAnswers(question, new string[0], returnAnswers); //показать вопрос и варианты ответов

            clientAnswer = Feedback.AnswerPlayerString(); // получение ответа клиента

            if (CheckСonditionsString(clientAnswer, regex, range)) break; // проверка ответа валидатором
        }
        return clientAnswer;
    }
}
