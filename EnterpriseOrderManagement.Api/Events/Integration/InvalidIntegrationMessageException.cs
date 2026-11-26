namespace EnterpriseOrderManagement.Api.Events.Integration;


//δηλώνουμε ρητά ότι έχουμε εντοπίσει παραβίαση ενός συγκεκριμένου κανόνα εγκυρότητας του μηνύματος
public sealed class InvalidIntegrationMessageException
    : Exception
{
    public InvalidIntegrationMessageException(string message)
        : base(message)
    {
    }

    public InvalidIntegrationMessageException(
        string message,
        Exception innerException)   //μας επιτρέπει να διατηρήσουμε πληροφορίες για το αρχικό σφάλμα, όταν υπάρχει
        : base(message, innerException)
    {
    }
}