using CondiService.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CondiService.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RepairRequest>()
            .HasOne(r => r.AssignedSpecialist)
            .WithMany()
            .HasForeignKey(r => r.AssignedSpecialistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<RepairRequest>()
            .HasOne(r => r.CustomerUser)
            .WithMany()
            .HasForeignKey(r => r.CustomerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<RequestComment>()
            .HasOne(c => c.AuthorUser)
            .WithMany()
            .HasForeignKey(c => c.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RequestComment>()
            .HasOne(c => c.RepairRequest)
            .WithMany(r => r.Comments)
            .HasForeignKey(c => c.RepairRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RepairRequest>()
            .HasIndex(r => r.CreatedAt);

        builder.Entity<RepairRequest>()
            .HasIndex(r => r.Status);
    }
}
