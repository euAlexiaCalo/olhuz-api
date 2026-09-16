using System;

namespace olhuz.API.Models.DTOs.User
{
	// Dados públicos do usuário.
	public class UserResponseDto
	{
		public int Id { get; set; }

		public string FullName { get; set; } = string.Empty;

		public string CPF { get; set; } = string.Empty;

		public DateTime BirthDate { get; set; }

		public string PhoneNumber { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; }

		public bool IsActive { get; set; }
	}
}