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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StokvelMember>()
            .HasKey(member => new { member.StokvelId, member.UserId });

        modelBuilder.Entity<StokvelMember>().HasOne<Stokvel>().WithMany().HasForeignKey(member => member.StokvelId);

        modelBuilder.Entity<StokvelMember>().HasOne<User>().WithMany().HasForeignKey(member => member.UserId);

        modelBuilder.Entity<Contribution>().HasKey(contribution => new
        {
            contribution.StokvelId,
            contribution.UserId,
            contribution.Cycle
        });

        modelBuilder.Entity<ContributionCycle>().HasOne<Stokvel>().WithMany().HasForeignKey(cycle => cycle.StokvelId);

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

        modelBuilder.Entity<Stokvel>()
            .Ignore(stokvel => stokvel.Members);
    }
}