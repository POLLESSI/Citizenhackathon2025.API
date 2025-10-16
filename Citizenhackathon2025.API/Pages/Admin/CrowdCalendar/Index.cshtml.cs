using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.API.Pages.Admin.CrowdCalendar
{
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ICrowdCalendarRepository _repo;
        public IndexModel(ICrowdCalendarRepository repo) => _repo = repo;

        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public string? Region { get; set; }
        public IEnumerable<CrowdCalendarEntry> Items { get; set; } = Enumerable.Empty<CrowdCalendarEntry>();

        public async Task OnGetAsync()
        {
            Items = await _repo.ListAsync(From, To, Region, null, null);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _repo.SoftDeleteAsync(id);
            return RedirectToPage(new { From, To, Region });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            await _repo.RestoreAsync(id);
            return RedirectToPage(new { From, To, Region });
        }
    }
}
