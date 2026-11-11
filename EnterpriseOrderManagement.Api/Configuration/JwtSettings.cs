namespace EnterpriseOrderManagement.Api.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";    

    public string Issuer { get; set; } = string.Empty;  //Ποιος εκδίδει το JWT

    public string Audience { get; set; } = string.Empty;    //Για ποιο API προορίζεται

    public string SecretKey { get; set; } = string.Empty;   //Το μυστικό κλειδί με το οποίο υπογράφουμε το JWT

    public int AccessTokenExpirationMinutes { get; set; } = 15; //Πόσο διαρκεί το access token

    public int RefreshTokenExpirationDays { get; set; } = 14;
}

