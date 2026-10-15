using System.Net;
using MatchDay.Booking.Api.Domain;

namespace MatchDay.Booking.Api.Catalog;

/// <summary>
/// Typed client for the catalog service. The base address uses Aspire service discovery
/// ("https+http://catalog-api"), and retries/timeouts come from the standard resilience handler.
/// </summary>
public sealed class CatalogClient(HttpClient httpClient)
{
    public async Task<SectionOffer?> GetSectionOfferAsync(int matchId, int sectionId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/matches/{matchId}/sections/{sectionId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SectionOffer>(cancellationToken);
    }
}

/// <summary>The catalog's price and seat layout for one section at one match.</summary>
public sealed record SectionOffer(
    int MatchId,
    string MatchTitle,
    DateTimeOffset KickOff,
    string Stadium,
    int SectionId,
    string SectionName,
    int Rows,
    int SeatsPerRow,
    decimal Price,
    string Currency)
{
    public bool HasSeat(Seat seat) => seat.Row <= Rows && seat.Number <= SeatsPerRow;

    public MatchSection ToMatchSection() =>
        new(MatchId, MatchTitle, KickOff, Stadium, SectionId, SectionName, Price);
}
