using Microsoft.EntityFrameworkCore;
using RondiTrack.Models;

namespace RondiTrack.Data;

public class RondiTrackDbContext : DbContext
{
    public RondiTrackDbContext(DbContextOptions<RondiTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Stokvel> Stokvels => Set<Stokvel>();
    public DbSet<StokvelMember> StokvelMembers => Set<StokvelMember>();
    public DbSet<ContributionCycle> ContributionCycles => Set<ContributionCycle>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Payout> Payouts => Set<Payout>();

    // Stores refresh tokens for authenticated users.
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(user => user.NormalizedEmail)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(user => user.PasswordHash)
            .HasMaxLength(1000);

        modelBuilder.Entity<StokvelMember>()
            .HasKey(member => new { member.StokvelId, member.UserId });

        modelBuilder.Entity<StokvelMember>()
            .HasOne(member => member.Stokvel)
            .WithMany(stokvel => stokvel.Members)
            .HasForeignKey(member => member.StokvelId);

        modelBuilder.Entity<StokvelMember>()
            .HasOne(member => member.User)
            .WithMany()
            .HasForeignKey(member => member.UserId);

        modelBuilder.Entity<Contribution>().HasKey(contribution => new
        {
            contribution.StokvelId,
            contribution.UserId,
            contribution.Cycle
        });

        modelBuilder.Entity<Contribution>()
            .HasOne(contribution => contribution.Member)
            .WithMany()
            .HasForeignKey(contribution => new
            {
                contribution.StokvelId,
                contribution.UserId
            });

        modelBuilder.Entity<ContributionCycle>()
        .HasOne(cycle => cycle.Stokvel)
        .WithMany(stokvel => stokvel.ContributionCycles)
        .HasForeignKey(cycle => cycle.StokvelId);

        modelBuilder.Entity<Contribution>().HasOne<User>().WithMany().HasForeignKey(contribution => contribution.UserId);

        modelBuilder.Entity<Contribution>().HasOne<Stokvel>().WithMany().HasForeignKey(contribution => contribution.StokvelId);

        modelBuilder.Entity<Contribution>()                        // For a Contribution
            .HasOne<ContributionCycle>()                           // it relates to one ContributionCycle
            .WithMany()                                            // a ContributionCycle can have many Contributions
            .HasForeignKey(contribution => contribution.Cycle);    // Cycle is the foreign key

        modelBuilder.Entity<Payout>()
            .HasOne<Stokvel>()
            .WithMany()
            .HasForeignKey(payout => payout.StokvelId);

        modelBuilder.Entity<Payout>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(payout => payout.UserId);

        modelBuilder.Entity<Payout>()
            .HasOne<ContributionCycle>()
            .WithMany()
            .HasForeignKey(payout => payout.ContributionCycleId);

        // Ensure each refresh-token hash is unique.
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        // Link each refresh token to its user.
        modelBuilder.Entity<RefreshToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId);

        // Require a token hash.
        modelBuilder.Entity<RefreshToken>()
            .Property(token => token.TokenHash)
            .IsRequired();

    }


}