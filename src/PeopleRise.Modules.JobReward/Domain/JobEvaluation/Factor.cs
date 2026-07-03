using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class Factor : Entity
{
    public Guid MethodologyVersionId { get; private set; }

    public MethodologyVersion? MethodologyVersion { get; private set; }

    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public decimal Weight { get; private set; } = 1m;

    public int SortOrder { get; private set; }

    public ICollection<Question>? Questions { get; private set; } //EF

    private Factor() { }   // EF

    public static Factor Create(MethodologyVersion version,
        string code,
        string nameEn,
        string? nameAr,
        decimal weight,
        int sortOrder)
    {
        return new()
        {
            MethodologyVersionId = version.Id,
            MethodologyVersion = version,
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            Weight = weight,
            SortOrder = sortOrder
        };
    }

    public void Update(string code, 
        string nameEn, 
        string? nameAr, 
        decimal weight, 
        int sortOrder)
    { 
        Code = code; 
        NameEn = nameEn; 
        NameAr = nameAr;
        Weight = weight; 
        SortOrder = sortOrder; 
    }

    public Question AddQuestion(string questionTextEn,
        string? questionTextAr,
        string? helpTextEn,
        string? helpTextAr,
        QuestionType questionType,
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
