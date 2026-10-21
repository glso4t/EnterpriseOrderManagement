namespace EnterpriseOrderManagement.Api.Dtos.Common;

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; }    //παραγγελίες της τρέχουσας σελίδας
        = Array.Empty<T>();

    public int Page { get; init; }  //αριθμός της σελίδας

    public int PageSize { get; init; }  //μέγεθος της σελίδας

    public int TotalCount { get; init; }    //το συνολικό πλήθος αποτελεσμάτων μετά τα φίλτρα

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);   //συνολικός αριθμός σελιδών
}