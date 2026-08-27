namespace ClinicManagementSystem.Domain.Enums;

public enum LabOrderStatus
{
    Ordered = 1,
    SampleCollected = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum LabOrderPriority
{
    Routine = 1,
    Urgent = 2,
    Stat = 3
}

public enum LabResultFlag
{
    Normal = 1,
    Low = 2,
    High = 3,
    Critical = 4
}
