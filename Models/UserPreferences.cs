using olhuz.API.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olhuz.API.Models
{
    [Table("TB_USER_PREFERENCES")]
    public class UserPreferences
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Leitor de tela ativado ou desativado
        public bool ScreenReader { get; set; }
        // Velocidade da fala
        public decimal SpeechRate { get; set; }
        // Tipo de voz - Masculina ou Feminina
        public VoiceType VoiceType { get; set; }
        // Nível do volume
        public int VolumeLevel { get; set; }
        // Modo de exibição - Claro ou Escuro
        public ThemeType Theme { get; set; }
        // Vibrações do app
        public bool VibrationEnabled { get; set; }
        // Sons de alerta
        public bool AlertSoundEnabled { get; set; }
        // Relacionamento: ID do usuário responsável por essa configuração
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public UserPreferences()
        {
            // Valores padrão de configurações iniciais
            ScreenReader = false;
            SpeechRate = 1.0m;
            VoiceType = VoiceType.Feminina;
            VolumeLevel = 50;
            Theme = ThemeType.Light;
            VibrationEnabled = true;
            AlertSoundEnabled = true;
        }
    }
}
