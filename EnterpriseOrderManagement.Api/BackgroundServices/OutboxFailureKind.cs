namespace EnterpriseOrderManagement.Api.BackgroundServices;

public enum OutboxFailureKind
{
    Unknown = 0,
    Infrastructure = 1,
    Message = 2
}