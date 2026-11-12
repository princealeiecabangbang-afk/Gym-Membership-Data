using GymManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.ToTable("membership_plans", table =>
            {
                table.HasCheckConstraint("CK_membership_plans_duration_days", "duration_days > 0");
                table.HasCheckConstraint("CK_membership_plans_price", "price >= 0");
                table.HasCheckConstraint("CK_membership_plans_is_active", "is_active IN (0, 1)");
            });
            entity.HasKey(plan => plan.PlanId);
            entity.Property(plan => plan.PlanId).HasColumnName("plan_id").ValueGeneratedOnAdd();
            entity.Property(plan => plan.PlanName).HasColumnName("plan_name").IsRequired();
            entity.Property(plan => plan.DurationDays).HasColumnName("duration_days").IsRequired();
            entity.Property(plan => plan.Price).HasColumnName("price").HasColumnType("DECIMAL(10, 2)").IsRequired();
            entity.Property(plan => plan.Description).HasColumnName("description");
            entity.Property(plan => plan.IsActive).HasColumnName("is_active").HasColumnType("INTEGER").HasDefaultValue(true);
            entity.HasIndex(plan => plan.PlanName).IsUnique();
            entity.HasMany(plan => plan.Members)
                .WithOne(member => member.Plan)
                .HasForeignKey(member => member.PlanId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(plan => plan.Payments)
                .WithOne(payment => payment.Plan)
                .HasForeignKey(payment => payment.PlanId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("members", table =>
            {
                table.HasCheckConstraint("CK_members_gender", "gender IN ('Male', 'Female', 'Other')");
                table.HasCheckConstraint("CK_members_status", "status IN ('active', 'inactive', 'suspended', 'cancelled')");
            });
            entity.HasKey(member => member.MemberId);
            entity.Property(member => member.MemberId).HasColumnName("member_id").ValueGeneratedOnAdd();
            entity.Property(member => member.FirstName).HasColumnName("first_name").IsRequired();
            entity.Property(member => member.LastName).HasColumnName("last_name").IsRequired();
            entity.Property(member => member.Email).HasColumnName("email").IsRequired();
            entity.Property(member => member.Phone).HasColumnName("phone");
            entity.Property(member => member.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("DATE");
            entity.Property(member => member.Gender).HasColumnName("gender");
            entity.Property(member => member.JoinDate).HasColumnName("join_date").HasColumnType("DATE").HasDefaultValueSql("CURRENT_DATE");
            entity.Property(member => member.Status).HasColumnName("status").HasDefaultValue("active").IsRequired();
            entity.Property(member => member.PlanId).HasColumnName("plan_id").IsRequired();
            entity.HasIndex(member => member.Email).IsUnique();
            entity.HasIndex(member => member.PlanId).HasDatabaseName("idx_members_plan_id");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendance");
            entity.HasKey(record => record.AttendanceId);
            entity.Property(record => record.AttendanceId).HasColumnName("attendance_id").ValueGeneratedOnAdd();
            entity.Property(record => record.MemberId).HasColumnName("member_id").IsRequired();
            entity.Property(record => record.AttendanceDate).HasColumnName("attendance_date").HasColumnType("DATE").IsRequired();
            entity.Property(record => record.CheckInTime).HasColumnName("check_in_time").HasColumnType("TEXT").IsRequired();
            entity.Property(record => record.CheckOutTime).HasColumnName("check_out_time").HasColumnType("TEXT");
            entity.Property(record => record.Notes).HasColumnName("notes");
            entity.HasIndex(record => new { record.MemberId, record.AttendanceDate })
                .HasDatabaseName("idx_attendance_member_date");
            entity.HasOne(record => record.Member)
                .WithMany(member => member.Attendances)
                .HasForeignKey(record => record.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments", table =>
            {
                table.HasCheckConstraint("CK_payments_amount", "amount >= 0");
                table.HasCheckConstraint("CK_payments_payment_method", "payment_method IN ('Cash', 'Card', 'Bank Transfer', 'Mobile Money', 'Other')");
                table.HasCheckConstraint("CK_payments_status", "status IN ('paid', 'pending', 'failed', 'refunded')");
            });
            entity.HasKey(payment => payment.PaymentId);
            entity.Property(payment => payment.PaymentId).HasColumnName("payment_id").ValueGeneratedOnAdd();
            entity.Property(payment => payment.MemberId).HasColumnName("member_id").IsRequired();
            entity.Property(payment => payment.PlanId).HasColumnName("plan_id").IsRequired();
            entity.Property(payment => payment.Amount).HasColumnName("amount").HasColumnType("DECIMAL(10, 2)").IsRequired();
            entity.Property(payment => payment.PaymentDate).HasColumnName("payment_date").HasColumnType("DATE").HasDefaultValueSql("CURRENT_DATE");
            entity.Property(payment => payment.PaymentMethod).HasColumnName("payment_method").IsRequired();
            entity.Property(payment => payment.Status).HasColumnName("status").HasDefaultValue("paid").IsRequired();
            entity.Property(payment => payment.ReceiptNumber).HasColumnName("receipt_number");
            entity.HasIndex(payment => payment.MemberId).HasDatabaseName("idx_payments_member_id");
            entity.HasIndex(payment => payment.PlanId).HasDatabaseName("idx_payments_plan_id");
            entity.HasIndex(payment => payment.ReceiptNumber).IsUnique();
            entity.HasOne(payment => payment.Member)
                .WithMany(member => member.Payments)
                .HasForeignKey(payment => payment.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}