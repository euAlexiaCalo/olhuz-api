using Microsoft.EntityFrameworkCore;
using olhuz.API.Models;

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

        // Método para configurarmos as regras adicionais que não estão diretamente nas Models
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Chama as configurações padão da classe DbContext
            base.OnModelCreating(modelBuilder);

            // ========================================
            // RELACIONAMENTO 1 PARA 1
            // ========================================

            // User ---------- UserPreferences

            // -> Um usuário possui apenas uma preferência.
            // -> Uma preferência pertence a apenas um usuário.

            // Inicia as configurações para a entidade User
            modelBuilder.Entity<User>()

                // Primeira ponta do relacionamento a partir de User
                .HasOne<UserPreferences>()

                // Segunda ponta do relacionamento a partir de UserPreferences
                .WithOne(p => p.User)

                // UserPreferences guarda a chave estrangeira que aponta para a tabela User
                .HasForeignKey<UserPreferences>(p => p.UserId);

            // CPF único
            modelBuilder.Entity<User>()
                .HasIndex(u => u.CPF)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            // E-mail único
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Inicia as configurações para a entidade UserPreferences
            modelBuilder.Entity<UserPreferences>()

                // Cria um índice para UserId
                .HasIndex(p => p.UserId)

                // Torna o valor único no banco
                .IsUnique();
        }
    }
}
