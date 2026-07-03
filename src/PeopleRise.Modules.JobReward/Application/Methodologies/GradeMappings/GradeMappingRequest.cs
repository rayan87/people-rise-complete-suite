namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

public record GradeMappingRequest(Guid GradeId, int MinScore, int MaxScore);