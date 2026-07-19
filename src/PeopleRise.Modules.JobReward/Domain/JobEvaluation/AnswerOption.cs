using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class AnswerOption : Entity   // unified rule: every question uses the same 1-5 rating scale
{
    public Guid QuestionId { get; private set; }

    public Question? Question { get; private set; }

    public string LabelEn { get; private set; } = "";

    public string? LabelAr { get; private set; }

    public string? HelpTextEn { get; private set; }

    public string? HelpTextAr { get; private set; }

    public int Rating { get; private set; }

    public int SortOrder { get; private set; }

    private AnswerOption() { }   // EF

    public static AnswerOption Create(Question question,
        string labelEn,
        string? labelAr,
        string? helpTextEn,
        string? helpTextAr,
        int rating,
        int sortOrder)
    {
        EnsureValidRating(rating);

        return new()
        {
            QuestionId = question.Id,
            Question = question,
            LabelEn = labelEn,
            LabelAr = labelAr,
            HelpTextEn = helpTextEn,
            HelpTextAr = helpTextAr,
            Rating = rating,
            SortOrder = sortOrder
        };
    }

    public void Update(string labelEn, string? labelAr, string? helpTextEn, string? helpTextAr, int rating, int sortOrder)
    {
        EnsureValidRating(rating);

        LabelEn = labelEn;
        LabelAr = labelAr;
        HelpTextEn = helpTextEn;
        HelpTextAr = helpTextAr;
        Rating = rating;
        SortOrder = sortOrder;
    }

    private static void EnsureValidRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new DomainException("Rating must be between 1 and 5.");
        }
    }
}
