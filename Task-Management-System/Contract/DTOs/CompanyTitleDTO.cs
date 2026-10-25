using System;
using System.ComponentModel.DataAnnotations;

namespace Contract.DTOs
{
    public class CompanyTitleDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? Voen { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int TotalTasksCount { get; set; }
        public int TotalProjectsCount { get; set; }
    }

    public class CreateCompanyTitleDTO
    {
        [Required(ErrorMessage = "Title (Holdinq / Şirkət) adı mütləq daxil edilməlidir.")]
        [StringLength(256, ErrorMessage = "Ad maksimum 256 simvol ola bilər.")]
        public string Name { get; set; } = default!;

        public string? Description { get; set; }
        public string? Voen { get; set; }
    }

    public class UpdateCompanyTitleDTO
    {
        [Required(ErrorMessage = "Title (Holdinq / Şirkət) adı mütləq daxil edilməlidir.")]
        [StringLength(256, ErrorMessage = "Ad maksimum 256 simvol ola bilər.")]
        public string Name { get; set; } = default!;

        public string? Description { get; set; }
        public string? Voen { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
