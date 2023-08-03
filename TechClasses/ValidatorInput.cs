namespace TechClasses;

//проверка корректности ввода данных
public class ValidatorInput
{    
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
    public static string GetAnswerString(string question, params string[] returnAnswers)
    {
        var _ = Feedback.ShowQuestionAnswers(question, new string[0], returnAnswers); //показать вопрос
        string clientAnswer = Feedback.AnswerPlayerString(); // получение ответа клиента        
        return clientAnswer;
    }

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
}
