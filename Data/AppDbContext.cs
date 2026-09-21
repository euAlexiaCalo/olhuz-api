using Microsoft.EntityFrameworkCore;
using olhuz.API.Models;
using olhuz.API.Models.Enums;

namespace olhuz.API.Data
{
    // Classe responsável por representar o banco de dados dentro da aplicação
    public class AppDbContext : DbContext
    {
        // Construtor responsável por receber as configurações do banco vindas do Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Tabela TB_USER
        public DbSet<User> Users { get; set; }

        // Tabela TB_USER_PREFERENCES
        public DbSet<UserPreferences> UserPreferences { get; set; }

        // Tabela TB_READING_HISTORY
        public DbSet<ReadingHistory> ReadingHistories { get; set; }

        // Método para configurarmos as regras adicionais que não estão diretamente nas Models
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Chama as configurações padão da classe DbContext
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // TABELA USER
            // ============================================================

            modelBuilder.Entity<User>(entity =>
            {
                // Nome da tabela
                entity.ToTable("TB_USER");

                // Chave primária
                entity.HasKey(u => u.Id);

                // ID gerado pelo SQL Server
                entity.Property(u => u.Id)
                    .HasDefaultValueSql("NEWID()");

                // Nome completo
                entity.Property(u => u.FullName)
                    .HasMaxLength(150)
                    .IsRequired();

                // CPF
                entity.Property(u => u.CPF)
                    .HasColumnType("varchar(11)")
                    .IsRequired();

                // Data de nascimento
                entity.Property(u => u.BirthDate)
                    .HasColumnType("date")
                    .IsRequired();

                // Telefone
                entity.Property(u => u.PhoneNumber)
                    .HasColumnType("varchar(15)")
                    .IsRequired();

                // E-mail
                entity.Property(u => u.Email)
                    .HasMaxLength(150)
                    .IsRequired();

                // Senha criptografada
                entity.Property(u => u.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();

                // Token de recuperação
                entity.Property(u => u.RecoveryToken)
                    .HasMaxLength(255);

                // Data de expiração do token
                entity.Property(u => u.TokenExpirationDate)
                    .HasColumnType("datetime2");

                // Token já utilizado
                entity.Property(u => u.TokenUsed)
                    .HasDefaultValue(false)
                    .IsRequired();

                // Data de criação
                entity.Property(u => u.CreatedAt)
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("GETUTCDATE()")
                    .IsRequired();

                // Usuário ativo/inativo
                entity.Property(u => u.IsActive)
                    .HasDefaultValue(true)
                    .IsRequired();

                // ÍNDICES ÚNICOS PARA USUÁRIOS ATIVOS

                // CPF só precisa ser único entre usuários ativos
                entity.HasIndex(u => u.CPF)
                    .IsUnique()
                    .HasFilter("[IsActive] = 1");

                // Telefone só precisa ser único entre usuários ativos
                entity.HasIndex(u => u.PhoneNumber)
                    .IsUnique()
                    .HasFilter("[IsActive] = 1");

                // E-mail só precisa ser único entre usuários ativos
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasFilter("[IsActive] = 1");

                // RELACIONAMENTO COM USER PREFERENCES
                // -> Um usuário possui apenas uma preferência.
                // -> Uma preferência pertence a apenas um usuário.
                entity.HasOne(u => u.Preferences)
                    .WithOne(p => p.User)
                    .HasForeignKey<UserPreferences>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================================
            // USER PREFERENCES
            // ============================================================

            modelBuilder.Entity<UserPreferences>(entity =>
            {
                // Nome da tabela
                entity.ToTable("TB_USER_PREFERENCES");

                // Chave primária
                entity.HasKey(p => p.Id);

                // ID gerado pelo SQL Server
                entity.Property(p => p.Id)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(p => p.ScreenReader)
                    .HasDefaultValue(false);

                // Velocidade da fala
                entity.Property(p => p.SpeechRate)
                    .HasPrecision(3, 1)
                    .HasDefaultValue(1.0m);

                entity.Property(p => p.VoiceType)
                    .HasDefaultValue(VoiceType.Feminina);

                entity.Property(p => p.VolumeLevel)
                    .HasDefaultValue(50);

                entity.Property(p => p.Theme)
                    .HasDefaultValue(ThemeType.Light);

                entity.Property(p => p.VibrationEnabled)
                    .HasDefaultValue(true);

                entity.Property(p => p.AlertSoundEnabled)
                    .HasDefaultValue(true);

                // UserId
                entity.Property(p => p.UserId)
                    .IsRequired();

                // Cada usuário pode ter apenas uma preferência
                entity.HasIndex(p => p.UserId)
                    .IsUnique();
            });

            // ============================================================
            // READING HISTORY
            // ============================================================
            modelBuilder.Entity<ReadingHistory>(entity =>
            {
                // Nome da tabela
                entity.ToTable("TB_READING_HISTORY");

                // Chave primária
                entity.HasKey(e => e.Id);

                // ID gerado pelo SQL Server
                entity.Property(e => e.Id)
                    .HasDefaultValueSql("NEWID()");

                // Tipo do arquivo (Imagem, Documento, etc.)
                entity.Property(e => e.Type)
                    .HasMaxLength(50)
                    .IsRequired();

                // Título
                entity.Property(e => e.Title)
                    .HasMaxLength(255)
                    .IsRequired();

                // Nome do arquivo original
                entity.Property(e => e.FileName)
                    .HasMaxLength(255);

                // Tamanho formatado do arquivo
                entity.Property(e => e.FileSize)
                    .HasMaxLength(50);

                // Caminho físico/URL do arquivo
                entity.Property(e => e.FilePath)
                    .HasMaxLength(500);

                // Data do upload
                entity.Property(e => e.UploadDate)
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("GETUTCDATE()")
                    .IsRequired();

                // Descrição gerada
                entity.Property(e => e.DescriptionText)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();

                // Configura a relação: Um usuário possui muitas leituras
                entity.HasOne(r => r.User)
                      .WithMany(u => u.ReadingHistories)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
