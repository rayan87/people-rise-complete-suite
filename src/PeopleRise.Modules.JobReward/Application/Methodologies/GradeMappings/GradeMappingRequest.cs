namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

// MinScore/MaxScore null = assign the grade without a range yet (step one of the two-step flow).
public record GradeMappingRequest(Guid GradeId, int? MinScore = null, int? MaxScore = null);
