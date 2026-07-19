using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class Factor : Entity
{
    public Guid MethodologyVersionId { get; private set; }

    public MethodologyVersion? MethodologyVersion { get; private set; }

    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public string? HelpTextEn { get; private set; }

    public string? HelpTextAr { get; private set; }

    /// <summary>Percentage weight of this factor within the version's point budget. Weights of all
    /// factors in a version must sum to 100 (enforced at Publish).</summary>
    public decimal Weight { get; private set; }

    public int SortOrder { get; private set; }

    public ICollection<Question>? Questions { get; private set; } //EF

    private Factor() { }   // EF

    public static Factor Create(MethodologyVersion version,
        string code,
        string nameEn,
        string? nameAr,
        string? helpTextEn,
        string? helpTextAr,
        decimal weight,
        int sortOrder)
    {
        EnsureValidWeight(weight);

        return new()
        {
            MethodologyVersionId = version.Id,
            MethodologyVersion = version,
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            HelpTextEn = helpTextEn,
            HelpTextAr = helpTextAr,
            Weight = weight,
            SortOrder = sortOrder
        };
    }

    public void Update(string code,
        string nameEn,
        string? nameAr,
        string? helpTextEn,
        string? helpTextAr,
        decimal weight,
        int sortOrder)
    {
        EnsureValidWeight(weight);

        Code = code;
        NameEn = nameEn;
        NameAr = nameAr;
        HelpTextEn = helpTextEn;
        HelpTextAr = helpTextAr;
        Weight = weight;
        SortOrder = sortOrder;
    }

    private static void EnsureValidWeight(decimal weight)
    {
        if (weight < 0 || weight > 100)
        {
            throw new DomainException("Factor weight must be between 0 and 100.");
        }
    }

    public Question AddQuestion(string questionTextEn,
        string? questionTextAr,
        string? helpTextEn,
        string? helpTextAr,
        QuestionType questionType,
        decimal weight,
        bool isRequired,
        int sortOrder)
    {
        if (this.MethodologyVersion is null)
        {
            throw new DomainException("Methodology version must be loaded first.");
        }

        this.MethodologyVersion.EnsureEditable();

        if (Questions is null)
        {
            Questions = [];
        }

        var question = Question.Create(this,
            questionTextEn,
            questionTextAr,
            helpTextEn,
            helpTextAr,
            questionType,
            weight,
            isRequired,
            sortOrder);

        Questions.Add(question);

        return question;
    }

    public Question? UpdateQuestion(Guid questionId,
        string questionTextEn,
        string? questionTextAr,
        string? helpTextEn,
        string? helpTextAr,
        QuestionType questionType,
        decimal weight,
        bool isRequired,
        int sortOrder)
    {
        if (this.MethodologyVersion is null)
        {
            throw new DomainException("Methodology version must be loaded first.");
        }

        this.MethodologyVersion.EnsureEditable();

        if (Questions is null)
        {
            throw new DomainException("Questions must be loaded first to update question.");
        }

        var question = Questions.FirstOrDefault(f => f.Id == questionId);

        if (question is null)
        {
            return null;
        }

        question.Update(questionTextEn,
            questionTextAr,
            helpTextEn,
            helpTextAr,
            questionType,
            weight,
            isRequired,
            sortOrder);

        return question;
    }

    public bool RemoveQuestion(Guid questionId)
    {
        if (this.MethodologyVersion is null)
        {
            throw new DomainException("Methodology version must be loaded first.");
        }

        this.MethodologyVersion.EnsureEditable();

        if (Questions is null)
        {
            throw new DomainException("Questions must be loaded first to remove the specified factorId.");
        }

        var question = Questions.FirstOrDefault(f => f.Id == questionId);

        if (question is null)
        {
            return false;
        }

        Questions.Remove(question);

        return true;
    }

}
