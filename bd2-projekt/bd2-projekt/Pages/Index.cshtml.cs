using bd2_projekt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace bd2_projekt.Pages;

public class IndexModel : PageModel
{
    public ReservationRequest Input { get; } = new();

    public IReadOnlyList<SelectListItem> EventTypes { get; } =
        ReservationRequest
            .AllowedEventTypes
            .Select(eventType => new SelectListItem(eventType, eventType))
            .ToArray();
}
