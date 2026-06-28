namespace PeopleRise.Modules.JobReward.Domain;

public enum JobStatus { Draft, Evaluated, Active, Archived }
public enum PositionStatus { ApprovedVacant, Filled, Frozen, Abolished }   // ApprovedVacant = the "open box"
public enum MethodologyVersionStatus { Draft, Active, Retired }
public enum EvaluationStatus { Draft, Submitted, Approved, Superseded }
public enum BandStatus { Draft, Published, Retired }
public enum Posture { Lead, Match, Lag }
public enum CompSource { ConsultingImport, Payroll }
