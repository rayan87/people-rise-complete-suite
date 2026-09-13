namespace PeopleRise.Modules.JobReward.Domain;

// JobStatus, GradeSource, PositionStatus moved to PeopleRise.Core.Domain (core extraction).
public enum MethodologyVersionStatus { Draft, Active, Retired }
public enum QuestionType { SingleChoice, MultipleChoice }
public enum EvaluationStatus { Draft, Submitted, Approved, Superseded }
public enum BandStatus { Draft, Published, Retired }
public enum BandProvenance { Designed, ManuallyEntered }   // Core Spec §9: Designed (Compensation) · ManuallyEntered (Core UI)
public enum Posture { Lead, Match, Lag }
public enum CompSource { ConsultingImport, Payroll }
