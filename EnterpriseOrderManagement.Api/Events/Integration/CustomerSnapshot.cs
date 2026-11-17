namespace EnterpriseOrderManagement.Api.Events.Integration;

// A copy of the customer data that consumers need,
// taken at the time the event occurred.
public sealed record CustomerSnapshot(
    string Name,
    string Email
);