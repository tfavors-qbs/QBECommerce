using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models
{
	public class ErrorLog
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string ErrorType { get; set; } = string.Empty; // e.g., "PunchOut Checkout", "Cart Management", "API Error"

		[Required]
		[MaxLength(500)]
		public string ErrorTitle { get; set; } = string.Empty;

		[Required]
		public string ErrorMessage { get; set; } = string.Empty;

		public string? StackTrace { get; set; }

		public string? AdditionalData { get; set; } // JSON string for flexible data storage

		// User context
		[MaxLength(450)]
		public string? UserId { get; set; }

		[MaxLength(256)]
		public string? UserEmail { get; set; }

		// Request context
		[MaxLength(2000)]
		public string? RequestUrl { get; set; }

		[MaxLength(10)]
		public string? HttpMethod { get; set; }

		[MaxLength(50)]
		public string? IpAddress { get; set; }

		[MaxLength(500)]
		public string? UserAgent { get; set; }

		// PunchOut specific fields
		[MaxLength(100)]
		public string? SessionId { get; set; }

		public int? StatusCode { get; set; }

		// Metadata
		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(50)]
		public string? Environment { get; set; } // Development, Staging, Production

		[Required]
		public bool IsResolved { get; set; } = false;

		public DateTime? ResolvedAt { get; set; }

		[MaxLength(450)]
		public string? ResolvedBy { get; set; }

		public string? ResolutionNotes { get; set; }

		// Relationships
		[ForeignKey("UserId")]
		public ApplicationUser? User { get; set; }
	}

	// View model for displaying errors
	public class ErrorLogViewModel
	{
		public int Id { get; set; }
		public string ErrorType { get; set; } = string.Empty;
		public string ErrorTitle { get; set; } = string.Empty;
		public string ErrorMessage { get; set; } = string.Empty;
		public string? StackTrace { get; set; }
		public string? AdditionalData { get; set; }
		public string? UserId { get; set; }
		public string? UserEmail { get; set; }
		public string? RequestUrl { get; set; }
		public string? HttpMethod { get; set; }
		public string? IpAddress { get; set; }
		public string? SessionId { get; set; }
		public int? StatusCode { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? Environment { get; set; }
		public bool IsResolved { get; set; }
		public DateTime? ResolvedAt { get; set; }
		public string? ResolvedBy { get; set; }
		public string? ResolutionNotes { get; set; }
	}
}
