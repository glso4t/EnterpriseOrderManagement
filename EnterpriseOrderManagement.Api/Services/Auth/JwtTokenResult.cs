namespace EnterpriseOrderManagement.Api.Services.Auth;

public record JwtTokenResult(   //Αντί να επιστρέφουμε μόνο ένα string, επιστρέφουμε και την ημερομηνία λήξης
    string AccessToken,
    DateTime ExpiresAtUtc
);