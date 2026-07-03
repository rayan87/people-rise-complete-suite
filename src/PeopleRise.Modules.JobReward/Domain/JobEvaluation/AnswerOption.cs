using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class AnswerOption : Entity   // simple rule: answer = points
{
    public Guid QuestionId { get; private set; }

    public Question? Question { get; private set; }

    public string LabelEn { get; private set; } = "";

    public string? LabelAr { get; private set; }

    public int Points { get; private set; }

    public int SortOrder { get; private set; }

    private AnswerOption() { }   // EF

    public static AnswerOption Create(Question question, 
        string labelEn, 
        string? labelAr, 
        int points, 
        int sortOrder)
    {
        return new() 
        {
            QuestionId = question.Id, 
            Question = question,
            LabelEn = labelEn, 
            LabelAr = labelAr, 
            Points = points, 
            SortOrder = sortOrder 
        };
    }
        
    public void Update(string labelEn, string? labelAr, int points, int sortOrder)
    { 
        LabelEn = labelEn; 
        LabelAr = labelAr; 
        Points = points; 
        SortOrder = sortOrder; 
    }
}