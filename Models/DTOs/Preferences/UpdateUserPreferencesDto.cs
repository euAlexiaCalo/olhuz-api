using System.ComponentModel.DataAnnotations;
using olhuz.API.Models.Enums;

namespace olhuz.API.Models.DTOs.Preferences
{
    // DTO responsável por atualizar as preferências do usuário.
    public class UpdateUserPreferencesDto
    {
        public bool ScreenReader { get; set; }

        [Range(0.5, 2.0,
            ErrorMessage = "A velocidade da fala deve estar entre 0.5 e 2.0.")]
        public decimal SpeechRate { get; set; }

        [Required(ErrorMessage = "O tipo de voz é obrigatório.")]
        public VoiceType VoiceType { get; set; }

        [Range(0, 100,
            ErrorMessage = "O volume deve estar entre 0 e 100.")]
        public int VolumeLevel { get; set; }

        [Required(ErrorMessage = "O tema é obrigatório.")]
        public ThemeType Theme { get; set; }

        public bool VibrationEnabled { get; set; }

        public bool AlertSoundEnabled { get; set; }
    }
}