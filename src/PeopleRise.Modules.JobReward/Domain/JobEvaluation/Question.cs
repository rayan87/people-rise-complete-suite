using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class Question : Entity
{
    public Guid FactorId { get; private set; }

    public Factor? Factor { get; private set; }

    public string QuestionTextEn { get; private set; } = "";

    public string? QuestionTextAr { get; private set; }

    public string? HelpTextEn { get; private set; }

    public string? HelpTextAr { get; private set; }

    public QuestionType QuestionType { get; private set; } = QuestionType.SingleChoice;

    /// <summary>Percentage weight of this question within its factor's points. Weights of all
    /// questions in a factor must sum to 100 (enforced at Publish).</summary>
    public decimal Weight { get; private set; }

    /// <summary>If false, the evaluator may skip this question; its points are redistributed
    /// equally across the factor's other questions at scoring time.</summary>
    public bool IsRequired { get; private set; } = true;

    public int SortOrder { get; private set; }

    public ICollection<AnswerOption>? AnswerOptions { get; private set; } //EF

    private Question() { }   // EF

    public static Question Create(Factor factor,
        string questionTextEn,
        string? questionTextAr,
        string? helpTextEn,
        string? helpTextAr,
        QuestionType questionType,
        decimal weight,
        bool isRequired,
        int sortOrder)
    {
        EnsureValidWeight(weight);

        return new()
        {
            FactorId = factor.Id,
            Factor = factor,
            QuestionTextEn = questionTextEn,
            QuestionTextAr = questionTextAr,
            HelpTextEn = helpTextEn,
            HelpTextAr = helpTextAr,
            QuestionType = questionType,
            Weight = weight,
            IsRequired = isRequired,
            SortOrder = sortOrder,
        };
    }

    public void Update(string questionTextEn,
        string? questionTextAr,
        string? helpTextEn,
        string? helpTextAr,
        QuestionType questionType,
        decimal weight,
        bool isRequired,
        int sortOrder)
    {
        EnsureValidWeight(weight);

        QuestionTextEn = questionTextEn;
        QuestionTextAr = questionTextAr;
        HelpTextEn = helpTextEn;
        HelpTextAr = helpTextAr;
        QuestionType = questionType;
        Weight = weight;
        IsRequired = isRequired;
        SortOrder = sortOrder;
    }

    private static void EnsureValidWeight(decimal weight)
    {
        if (weight < 0 || weight > 100)
        {
            throw new DomainException("Question weight must be between 0 and 100.");
        }
    }

    public AnswerOption AddAnswerOption(string labelEn,
        string? labelAr,
        string? helpTextEn,
        string? helpTextAr,
        int rating,
        int sortOrder)
    {
        if (this.Factor?.MethodologyVersion is null)
        {
            throw new DomainException("Factor and methodology version must be loaded first.");
        }

        this.Factor.MethodologyVersion.EnsureEditable();

        if (AnswerOptions is null)
        {
            AnswerOptions = [];
        }

        var answerOption = AnswerOption.Create(this,
            labelEn,
            labelAr,
            helpTextEn,
            helpTextAr,
            rating,
            sortOrder);

        AnswerOptions.Add(answerOption);

        return answerOption;
    }

    public AnswerOption? UpdateAnswerOption(Guid answerOptionId,
        string labelEn,
        string? labelAr,
        string? helpTextEn,
        string? helpTextAr,
        int rating,
        int sortOrder)
    {
        if (this.Factor?.MethodologyVersion is null)
        {
            throw new DomainException("Factor and methodology version must be loaded first.");
        }

        this.Factor.MethodologyVersion.EnsureEditable();

        if (AnswerOptions is null)
        {
            throw new DomainException("AnswerOptions must be loaded first to update answer option.");
        }

        var answerOption = AnswerOptions.FirstOrDefault(f => f.Id == answerOptionId);

        if (answerOption is null)
        {
            return null;
        }

        answerOption.Update(labelEn,
            labelAr,
            helpTextEn,
            helpTextAr,
            rating,
            sortOrder);

        return answerOption;
    }

    public bool RemoveAnswerOption(Guid answerOptionId)
    {
        if (this.Factor?.MethodologyVersion is null)
        {
            throw new DomainException("Factor and methodology version must be loaded first.");
        }

        this.Factor.MethodologyVersion.EnsureEditable();

        if (AnswerOptions is null)
        {
            throw new DomainException("AnswerOptions must be loaded first to remove the specified answerOptionId.");
        }

        var answerOption = AnswerOptions.FirstOrDefault(f => f.Id == answerOptionId);

        if (answerOption is null)
        {
            return false;
        }

        AnswerOptions.Remove(answerOption);

        return true;
    }
}
