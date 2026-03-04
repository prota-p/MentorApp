using MentorApp.Domain.Models.Mentorships;
using MentorApp.Domain.Models.Topics;
using MentorApp.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace MentorApp.Infrastructure.Persistence;

/// <summary>
/// アプリケーションのDbContext
/// </summary>
/// <remarks>
/// EF Coreの規約を重視し、規約で設定できないものはFluent APIで設定する。
/// </remarks>
internal class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Mentorship> Mentorships => Set<Mentorship>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureMentorship(modelBuilder);
        ConfigureTopic(modelBuilder);
        ConfigureMessage(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Id はアプリ側で生成されるため、ValueGeneratedNeverを指定
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(e => e.ExternalId)
                .HasMaxLength(User.ExternalIdMaxLength);

            // ExternalIdはIdP切り替え時も一意であるべき
            entity.HasIndex(e => e.ExternalId)
                .IsUnique();

            entity.Property(e => e.DisplayName)
                .HasMaxLength(User.DisplayNameMaxLength);

            entity.Property(e => e.Email)
                .HasMaxLength(Email.MaxLength)
                .HasConversion(
                    email => email.Value,
                    value => new Email(value));
        });
    }

    private static void ConfigureMentorship(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mentorship>(entity =>
        {
            // Id はアプリ側で生成されるため、ValueGeneratedNeverを指定
            entity.Property(x => x.Id).ValueGeneratedNever();

            // User削除時の連鎖削除を防ぐ（同一集約ではない）
            entity.HasOne(e => e.MentorUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.MenteeUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Status)
                .HasConversion<int>();
        });
    }

    private static void ConfigureTopic(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Topic>(entity =>
        {
            // Id はアプリ側で生成されるため、ValueGeneratedNeverを指定
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(e => e.Title)
                .HasMaxLength(Topic.TitleMaxLength);

            // Mentorship削除時の連鎖削除を防ぐ（同一集約ではない）
            entity.HasOne(e => e.Mentorship)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Status)
                .HasConversion<int>();

            // Topic削除時に同一集約であるMessagesも連鎖削除する
            entity.HasMany(e => e.Messages)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            // Id はアプリ側で生成されるため、ValueGeneratedNeverを指定
            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(e => e.Content)
                .HasMaxLength(Message.ContentMaxLength);

            // User削除時の連鎖削除を防ぐ（同一集約ではない）
            entity.HasOne(e => e.SenderUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
