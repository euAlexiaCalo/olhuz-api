using olhuz.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.Responses.Preferences
{
    public class UserPreferencesResponse
    {
        public bool ScreenReader { get; set; }

        public decimal SpeechRate { get; set; }

        public VoiceType VoiceType { get; set; }

        public int VolumeLevel { get; set; }

        public ThemeType Theme { get; set; }

        public bool VibrationEnabled { get; set; }

        public bool AlertSoundEnabled { get; set; }
    }
}
