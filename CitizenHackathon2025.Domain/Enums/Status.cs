using System.ComponentModel.DataAnnotations;

namespace CitizenHackathon2025.Domain.Enums
{
    public enum Status
    {
        [Display(Name = "On hold")]
        Pending = 0,      // Awaiting processing or validation
        [Display(Name = "Active")]
        Active = 1,       // Active and operational
        [Display(Name = "Inactive")]
        Inactive = 2,     // Temporarily disabled
        [Display(Name = "Suspended")]
        Suspended = 3,    // Suspended for specific reasons
        [Display(Name = "Archive")]
        Archived = 4,     // Archived, no longer displayed but still in the database
        [Display(Name = "Deleted")]
        Deleted = 5       // Deleted (soft delete)
    }
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.