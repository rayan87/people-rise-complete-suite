namespace PeopleRise.Modules.JobReward.Domain;

// JobStatus, GradeSource, PositionStatus, BandStatus, BandProvenance, PayBasis moved to
// PeopleRise.Core.Domain - SalaryBand itself is a core entity (Core Spec §9).
public enum MethodologyVersionStatus { Draft, Active, Retired }
public enum QuestionType { SingleChoice, MultipleChoice }
public enum EvaluationStatus { Draft, Submitted, Approved, Superseded }
public enum Posture { Lead, Match, Lag }
public enum CompSource { ConsultingImport, Payroll }
